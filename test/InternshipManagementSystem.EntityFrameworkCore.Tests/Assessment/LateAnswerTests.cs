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
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// A recording that was already on its way when the clock ran out.
/// <para>
/// Somebody answering a speaking question talks until they are told to stop,
/// and a minute of audio is close to a megabyte. It is still travelling when
/// the deadline passes — and the product used to accept the file into storage
/// and then refuse the save that would have attached it to an answer.
/// </para>
/// <para>
/// So the recording existed on disk and nowhere else. The marker saw an empty
/// answer, the attempt was marked as needing no human at all, and the candidate
/// was scored zero for an answer they had given. Nobody would ever have found
/// out: there was no error, and nothing said a file had been orphaned.
/// </para>
/// </summary>
public class LateAnswerTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000c1");

    public LateAnswerTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_recording_that_arrives_just_after_the_bell_is_kept()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("late-a");
            var question = await _taking.GetQuestionAsync(sitting.Token, 0);

            await ExpireAsync(sitting.AttemptId, secondsAgo: 5);

            var result = await _taking.SaveAnswerAsync(sitting.Token, new SaveAnswerDto
            {
                QuestionId = question.Id,
                AnswerBlobName = "t/answers/a/recording.webm",
                AnswerFileName = "recording.webm",
            });

            // Both at once, and they are not opposites: the paper is over, and
            // the file is kept. Saying the paper was not over would hand back
            // time the candidate does not have.
            result.IsExpired.ShouldBeTrue();
            result.Saved.ShouldBeTrue();

            var answers = GetRequiredService<IRepository<Answer, Guid>>();
            var stored = await answers.FirstOrDefaultAsync(a => a.AttemptId == sitting.AttemptId);

            stored.ShouldNotBeNull();
            stored!.AnswerBlobName.ShouldBe("t/answers/a/recording.webm");
        });
    }

    [Fact]
    public async Task Typing_after_the_bell_is_still_refused()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("late-b");
            var question = await _taking.GetQuestionAsync(sitting.Token, 0);

            await ExpireAsync(sitting.AttemptId, secondsAgo: 5);

            var result = await _taking.SaveAnswerAsync(sitting.Token, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "written after time was up",
            });

            // The half that keeps the grace from being a loophole. Somebody still
            // typing after the bell is a different thing entirely from a file
            // that was already in flight, and this must never become a way to do
            // the first under cover of the second.
            result.IsExpired.ShouldBeTrue();
            result.Saved.ShouldBeFalse();

            var answers = GetRequiredService<IRepository<Answer, Guid>>();

            (await answers.FirstOrDefaultAsync(a => a.AttemptId == sitting.AttemptId)).ShouldBeNull();
        });
    }

    [Fact]
    public async Task Long_after_the_bell_even_a_file_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("late-c");
            var question = await _taking.GetQuestionAsync(sitting.Token, 0);

            // Well past the grace: not a file still travelling, but somebody
            // sending one an hour later.
            await ExpireAsync(sitting.AttemptId, secondsAgo: 3600);

            var result = await _taking.SaveAnswerAsync(sitting.Token, new SaveAnswerDto
            {
                QuestionId = question.Id,
                AnswerBlobName = "t/answers/a/much-later.webm",
                AnswerFileName = "much-later.webm",
            });

            result.Saved.ShouldBeFalse();
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task ExpireAsync(Guid attemptId, int secondsAgo)
    {
        var attempts = GetRequiredService<IRepository<Attempt, Guid>>();
        var attempt = await attempts.GetAsync(attemptId);

        attempt.DeadlineAt = DateTime.Now.AddSeconds(-secondsAgo);

        await attempts.UpdateAsync(attempt, autoSave: true);
    }

    private async Task<(Guid AttemptId, string Token)> SitAsync(string code)
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

        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.AudioResponse,
            Text = code + " — say your answer aloud",
            Score = 10m,
            Payload = PayloadJson.Write(new RubricPayload()),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Still speaking",
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
