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
using InternshipManagementSystem.Settings;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
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

    [Fact]
    public async Task An_organisation_that_turned_recording_off_is_not_recorded()
    {
        await AsTenantAsync(async () =>
        {
            var settings = GetRequiredService<ISettingManager>();

            await settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.CollectIntegritySignals, "false");

            var sitting = await SitAsync("signal-d");

            await _taking.ReportSignalAsync(sitting.SessionToken, new ReportIntegritySignalDto
            {
                Type = IntegritySignalType.WindowBlur,
                Magnitude = 30,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            // The switch is on a screen, its hint says what it does, and nothing
            // read it — so a centre that turned observation off observed anyway.
            // Watching people who were told they were not being watched is not a
            // defaulting bug; it is the promise being false.
            report.Signals.ShouldBeEmpty();

            await settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.CollectIntegritySignals, "true");
        });
    }

    [Fact]
    public async Task Text_arriving_faster_than_anyone_types_is_noted()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-e");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            // 600 characters in 20 seconds is thirty a second. A fast
            // touch-typist sustains about seven.
            await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = new string('x', 600),
                TimeSpentSeconds = 20,
                KeystrokeCount = 600,
                BackspaceCount = 4,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            report.Signals.ShouldContain(s => s.Type == IntegritySignalType.ImplausibleSpeed);
        });
    }

    [Fact]
    public async Task Somebody_writing_at_a_human_speed_is_not_noted()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-f");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            // The same 600 characters over eight minutes, with corrections. This
            // is what writing an answer looks like.
            await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = new string('x', 600),
                TimeSpentSeconds = 480,
                KeystrokeCount = 700,
                BackspaceCount = 40,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            // The half that decides whether the other half is worth anything. An
            // observation a marker cannot trust trains them to skim past all of
            // them, including the one that mattered.
            report.Signals.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_long_answer_typed_without_one_correction_is_noted()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-g");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = new string('y', 400),
                TimeSpentSeconds = 300,
                KeystrokeCount = 400,
                BackspaceCount = 0,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            report.Signals.ShouldContain(s => s.Type == IntegritySignalType.NoCorrections);
        });
    }

    [Fact]
    public async Task One_event_is_not_described_three_times()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-h");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            // Text that arrived at once is infinitely fast and has no
            // corrections, so both of those rules would otherwise fire on it.
            await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = new string('z', 500),
                WasPasted = true,
                TimeSpentSeconds = 3,
                KeystrokeCount = 0,
                BackspaceCount = 0,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            // This assertion used to also require a Paste signal from the save,
            // and that half is deliberately gone: pasting is blocked, so no text
            // arrives that way and the save has nothing to report. The attempt is
            // recorded by the browser when it happens.
            //
            // What the test was really for survives untouched and is the reason
            // it still exists — several sentences about one event read as several
            // findings, and a marker counting flags weighs it that many times.
            report.Signals.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task One_blocked_paste_is_one_record_however_often_the_paper_saves()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-paste-once");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            // The paper autosaves roughly every 800ms, and the flag stays set for
            // the rest of the question. So this is one blocked Ctrl+V followed by
            // a candidate carrying on typing — six saves, one event.
            for (var save = 1; save <= 6; save++)
            {
                await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = new string('z', 100 * save + 100),
                    TimeSpentSeconds = 60 * save,
                    KeystrokeCount = 120 * save,
                    BackspaceCount = 8 * save,
                    WasPasted = true,
                });
            }

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            // Nothing was pasted: the browser refused it, so the text never
            // reached the box. Recording it here — once per save, each carrying a
            // magnitude equal to the candidate's own typing — put a dozen
            // quantified accusations of an event that never happened in front of
            // the marker, and buried the honest signals under them.
            report.Signals.ShouldNotContain(signal => signal.Type == IntegritySignalType.Paste);
        });
    }

    [Fact]
    public async Task Trying_to_paste_is_still_recorded_once_by_the_browser()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("signal-paste-attempt");

            // The half that keeps the removal honest. Dropping the save-time
            // record must not make an attempt to paste invisible — the attempt is
            // real and the marker is entitled to know it happened.
            await _taking.ReportSignalAsync(sitting.SessionToken, new ReportIntegritySignalDto
            {
                Type = IntegritySignalType.Paste,
            });

            await _taking.SubmitAsync(sitting.SessionToken);

            var report = await _review.GetIntegrityReportAsync(sitting.AttemptId);

            report.Signals.Count(signal => signal.Type == IntegritySignalType.Paste).ShouldBe(1);
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
