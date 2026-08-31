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
using InternshipManagementSystem.Settings;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Feedback written to somebody who could never read it.
/// <para>
/// The marking screen labels the box "Feedback for the candidate" and says
/// underneath, in both languages, that it is shown to them with their result —
/// "so it is feedback rather than a note to yourself". It was stored on the
/// answer and carried nowhere: the candidate's result had no field for it at
/// all.
/// </para>
/// <para>
/// So every marker who took the trouble to write something wrote it to nobody,
/// and went on doing it, because the screen kept telling them otherwise. This
/// is the recurring defect in this product with the clearest cost: work a
/// person did, thrown away, by a screen that promised the opposite.
/// </para>
/// </summary>
public class MarkerFeedbackTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IReviewAppService _review;
    private readonly ISettingManager _settings;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000e1");

    public MarkerFeedbackTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _review = GetRequiredService<IReviewAppService>();
        _settings = GetRequiredService<ISettingManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task What_the_marker_wrote_reaches_the_person_it_was_written_for()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SatAsync("feedback-a");

            var pending = await _review.GetAnswersAsync(sitting.AttemptId);

            await _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = pending.Single().AnswerId,
                AwardedScore = 6m,
                Comment = "الفكرة صحيحة، لكن اربطها بالشاهد في السطر الثالث.",
            });

            var result = await _taking.GetResultAsync(sitting.Token);

            result.Feedback.ShouldContain("الفكرة صحيحة، لكن اربطها بالشاهد في السطر الثالث.");
        });
    }

    [Fact]
    public async Task A_sitting_nobody_wrote_on_carries_nothing()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SatAsync("feedback-b");

            var pending = await _review.GetAnswersAsync(sitting.AttemptId);

            await _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = pending.Single().AnswerId,
                AwardedScore = 6m,
            });

            var result = await _taking.GetResultAsync(sitting.Token);

            // A block that appears with nothing in it reads as a fault, and a
            // candidate who was given no feedback should be told nothing rather
            // than shown an empty heading where feedback would be.
            result.Feedback.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task An_organisation_that_withholds_the_score_withholds_the_words_too()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SatAsync("feedback-c");

            var pending = await _review.GetAnswersAsync(sitting.AttemptId);

            await _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = pending.Single().AnswerId,
                AwardedScore = 6m,
                Comment = "This should not reach them before the centre has seen it.",
            });

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.ShowResultToCandidate, "false");

            var result = await _taking.GetResultAsync(sitting.Token);

            // Half a released result is not a compromise, it is the same problem
            // the withholding setting exists to prevent: a centre that releases
            // marks itself has not agreed to feedback arriving ahead of them.
            result.ScoreWithheld.ShouldBeTrue();
            result.Feedback.ShouldBeEmpty();

            await _settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.ShowResultToCandidate, "true");
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<(Guid AttemptId, string Token)> SatAsync(string code)
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

        // Free text, so a person marks it and has somewhere to write.
        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.Text,
            Text = code + " — explain your reasoning",
            Score = 10m,
            Payload = PayloadJson.Write(new RubricPayload()),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Wrote something back",
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
            Response = "Because the level held on the retest.",
            TimeSpentSeconds = 240,
            KeystrokeCount = 80,
            BackspaceCount = 9,
        });

        await _taking.SubmitAsync(state.SessionToken!);

        return (state.AttemptId, state.SessionToken!);
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
