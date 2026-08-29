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
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// What the marker is told a candidate did.
/// <para>
/// These are observations, not accusations — leaving the tab is not cheating, a
/// phone rings — and the whole design rests on a person weighing them. Which
/// means the one thing they must be is <i>true</i>. A wrong observation does not
/// degrade gracefully into a vaguer one; it becomes a specific false claim about
/// a named candidate, sitting in the record, in front of the one person deciding
/// whether their answer was their own work.
/// </para>
/// <para>
/// It was wrong. The browser posted <c>{ kind: 'window-blur' }</c> to a server
/// reading <c>Type</c>, so nothing bound and every signal was stored as the
/// enum's default — Paste. Everybody who ever alt-tabbed was recorded as having
/// pasted.
/// </para>
/// </summary>
public class IntegritySignalTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IReviewAppService _review;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000071");

    public IntegritySignalTests()
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
    public async Task Leaving_the_window_is_recorded_as_leaving_the_window()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-a");

            await _taking.ReportSignalAsync(sitting.SessionToken, new ReportIntegritySignalDto
            {
                Type = IntegritySignalType.WindowBlur,
                Magnitude = 12,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            report.Signals.Single().Type.ShouldBe(IntegritySignalType.WindowBlur);

            // The sentence the marker actually reads. Asserting the stored enum
            // alone would have passed even while the report said something else.
            report.Observations.ShouldContain(o => o.Contains("Left the exam window"));
            report.Observations.ShouldNotContain(o => o.Contains("paste"));
        });
    }

    [Fact]
    public async Task Nothing_observed_is_nothing_reported()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-b");

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            // A candidate who did nothing unusual must arrive at the marker with
            // an empty report, not with a heading that implies there is something
            // to weigh.
            report.Signals.ShouldBeEmpty();
            report.Observations.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_short_paste_is_not_worth_reporting()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-c");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "a",
                WasPasted = true,
                TimeSpentSeconds = 3,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            // The threshold is deliberate: a paste long enough to be an imported
            // answer is worth a marker's attention, and pasting one word is not.
            // The browser used to report every paste separately as well, which
            // put back exactly the noise this keeps out.
            report.Signals.ShouldBeEmpty();
        });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record Sitting(Guid AttemptId, string SessionToken);

    private async Task<Sitting> SitAsync(string code)
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
            Type = QuestionTypes.Text,
            Text = code + " question",
            Score = 1m,
            Payload = PayloadJson.Write(new RubricPayload()),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Alt-tabbed once",
            Email = code + "@example.test",
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        var token = result.Recipients.Single().Url.Split('/').Last();
        var preview = await _taking.OpenLinkAsync(token);
        var state = await _taking.StartAsync(preview.SessionToken!);

        return new Sitting(state.AttemptId, state.SessionToken!);
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
