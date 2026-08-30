using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using InternshipManagementSystem.Assessment.Review;
using InternshipManagementSystem.Assessment.Review.Dtos;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Putting a mark right.
/// <para>
/// An attempt left the review queue the moment its last answer was marked, and
/// nothing brought it back. So a marker who typed 7 where they meant 17 had no
/// route to that sitting at all: the queue no longer listed it, and there is no
/// other screen that reaches one.
/// </para>
/// <para>
/// A mark is a person's judgement, and people revise judgements. Making the
/// revision impossible does not make the first mark more correct — only
/// permanent, which is a different thing, and the difference is somebody's
/// result.
/// </para>
/// </summary>
public class RemarkTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IReviewAppService _review;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000111");

    public RemarkTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _review = GetRequiredService<IReviewAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_marked_sitting_can_be_found_again_and_the_mark_corrected()
    {
        await AsTenantAsync(async () =>
        {
            var attemptId = await SatAndSubmittedAsync("remark-a");

            var pending = await _review.GetAnswersAsync(attemptId);
            var answer = pending.Single();

            // The mistype: seven where seventeen was meant, out of twenty.
            await _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = answer.AnswerId,
                AwardedScore = 7m,
            });

            // It is gone from what is waiting — correctly, it has been marked.
            var waiting = await _review.GetQueueAsync(new ReviewQueueRequestDto { MaxResultCount = 50 });

            waiting.Items.ShouldNotContain(item => item.AttemptId == attemptId);

            // And this is what was missing: a way back to it.
            var done = await _review.GetQueueAsync(
                new ReviewQueueRequestDto { Finished = true, MaxResultCount = 50 });

            done.Items.ShouldContain(item => item.AttemptId == attemptId);

            await _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = answer.AnswerId,
                AwardedScore = 17m,
            });

            // The total follows the correction. A re-mark that left the old total
            // standing would be worse than no re-mark: the marker would believe
            // it had been put right.
            var attempts = GetRequiredService<IRepository<Attempt, Guid>>();
            var after = await attempts.GetAsync(attemptId);

            after.Score.ShouldBe(17m);
        });
    }

    [Fact]
    public async Task What_is_waiting_is_still_what_is_waiting()
    {
        await AsTenantAsync(async () =>
        {
            var attemptId = await SatAndSubmittedAsync("remark-b");

            var waiting = await _review.GetQueueAsync(new ReviewQueueRequestDto { MaxResultCount = 50 });

            waiting.Items.ShouldContain(item => item.AttemptId == attemptId);

            // The half that keeps the change honest: adding a way to look back
            // must not put marked sittings into the list of work to do. A queue
            // that shows finished work is a queue nobody trusts to be a queue.
            var done = await _review.GetQueueAsync(
                new ReviewQueueRequestDto { Finished = true, MaxResultCount = 50 });

            done.Items.ShouldNotContain(item => item.AttemptId == attemptId);
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Guid> SatAndSubmittedAsync(string code)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
        });

        // Free text, so it waits for a person and there is a mark to get wrong.
        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.Text,
            Text = code + " — explain your reasoning",
            Score = 20m,
            Payload = PayloadJson.Write(new RubricPayload()),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Marked in a hurry",
            Email = code + "@example.test",
        });

        var sent = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        var token = sent.Recipients.Single().Url.Split('/').Last();
        var preview = await _taking.OpenLinkAsync(token);
        var state = await _taking.StartAsync(preview.SessionToken!);
        var question = await _taking.GetQuestionAsync(state.SessionToken!, 0);

        await _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
        {
            QuestionId = question.Id,
            Response = "Because the volume dried up at the high.",
            TimeSpentSeconds = 200,
            KeystrokeCount = 60,
            BackspaceCount = 5,
        });

        await _taking.SubmitAsync(state.SessionToken!);

        return state.AttemptId;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
