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
using InternshipManagementSystem.Settings;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// What the roll says about each person on it.
/// <para>
/// The status column was stored, defaulted, and assigned by nothing anywhere. So
/// it read «لم يُدعَ» — <i>not invited</i> — beside people who had sat the exam
/// and submitted it, and the filter above it could only return everybody or
/// nobody. The browser had its own <c>CandidateStatus</c> with entirely
/// different members; the two shared a name and agreed on nothing.
/// </para>
/// <para>
/// It is derived now, from the three facts that actually record it. These assert
/// that the column and the filter say the same thing, because a filter that
/// disagrees with the column beside it is how somebody concludes the roll is
/// broken and stops using it.
/// </para>
/// </summary>
public class CandidateStatusTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000081");

    private Guid _examId;

    public CandidateStatusTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Somebody_nobody_has_written_to_is_pending()
    {
        await AsTenantAsync(async () =>
        {
            await PublishExamAsync("status-a");
            var person = await PersonAsync("status-a");

            (await StatusOfAsync(person)).ShouldBe(CandidateStatus.Pending);
            await OnlyMatchesAsync(CandidateStatus.Pending, person);
        });
    }

    [Fact]
    public async Task Somebody_holding_an_unopened_link_is_invited()
    {
        await AsTenantAsync(async () =>
        {
            await PublishExamAsync("status-b");
            var person = await PersonAsync("status-b");

            await SendAsync(person);

            (await StatusOfAsync(person)).ShouldBe(CandidateStatus.Invited);
            await OnlyMatchesAsync(CandidateStatus.Invited, person);
        });
    }

    [Fact]
    public async Task Somebody_part_way_through_is_in_progress()
    {
        await AsTenantAsync(async () =>
        {
            await PublishExamAsync("status-c");
            var person = await PersonAsync("status-c");

            var token = await SendAsync(person);
            var preview = await _taking.OpenLinkAsync(token);

            await _taking.StartAsync(preview.SessionToken!);

            (await StatusOfAsync(person)).ShouldBe(CandidateStatus.InProgress);
            await OnlyMatchesAsync(CandidateStatus.InProgress, person);
        });
    }

    [Fact]
    public async Task Somebody_who_has_submitted_is_completed()
    {
        await AsTenantAsync(async () =>
        {
            await PublishExamAsync("status-d");
            var person = await PersonAsync("status-d");

            var token = await SendAsync(person);
            var preview = await _taking.OpenLinkAsync(token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await _taking.SubmitAsync(state.SessionToken!);

            // The case that was wrong in the worst way: this person read as
            // "not invited" on a screen a coordinator uses to decide who still
            // has to sit.
            (await StatusOfAsync(person)).ShouldBe(CandidateStatus.Completed);
            await OnlyMatchesAsync(CandidateStatus.Completed, person);
        });
    }

    [Fact]
    public async Task Having_sat_once_outranks_a_later_invitation()
    {
        await AsTenantAsync(async () =>
        {
            await PublishExamAsync("status-e");
            var person = await PersonAsync("status-e");

            var token = await SendAsync(person);
            var preview = await _taking.OpenLinkAsync(token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            await _taking.SubmitAsync(state.SessionToken!);

            // A resit, sent but not opened. Both facts are true at once.
            await SendAsync(person);

            // The more advanced one wins. A coordinator scanning the roll is
            // looking for who still has to sit, and burying that under a later
            // invitation is what makes the column useless.
            (await StatusOfAsync(person)).ShouldBe(CandidateStatus.Completed);
        });
    }

    [Fact]
    public async Task Withdrawn_matches_nobody_rather_than_everybody()
    {
        await AsTenantAsync(async () =>
        {
            await PublishExamAsync("status-f");
            await PersonAsync("status-f");

            var page = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                Status = CandidateStatus.Withdrawn,
                MaxResultCount = 100,
            });

            // Nothing in the product records a withdrawal, so nothing can match
            // one. An empty page is the honest answer; quietly returning
            // everybody is the old defect wearing a different hat.
            page.Items.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task An_organisation_that_releases_results_itself_shows_the_candidate_none()
    {
        await AsTenantAsync(async () =>
        {
            var settings = GetRequiredService<ISettingManager>();

            await settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.ShowResultToCandidate, "false");

            await PublishExamAsync("status-g");
            var person = await PersonAsync("status-g");

            var token = await SendAsync(person);
            var preview = await _taking.OpenLinkAsync(token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await _taking.SubmitAsync(state.SessionToken!);

            var result = await _taking.GetResultAsync(state.SessionToken!);

            // The setting's own hint promises this in writing: a certificate that
            // arrives before the coordinator has seen it is hard to withdraw. It
            // was written on a screen and read by nothing, so every candidate saw
            // their score the moment marking finished.
            result.ScoreWithheld.ShouldBeTrue();
            result.Score.ShouldBe(0m);
            result.TopicBreakdown.ShouldBeEmpty();

            await settings.SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.ShowResultToCandidate, "true");
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<CandidateStatus> StatusOfAsync(Guid id) =>
        (await _candidates.GetAsync(id)).Status;

    /// <summary>The filter agrees with the column: this person, and under no other status.</summary>
    private async Task OnlyMatchesAsync(CandidateStatus expected, Guid id)
    {
        foreach (var status in Enum.GetValues<CandidateStatus>())
        {
            var page = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                Status = status,
                MaxResultCount = 200,
            });

            var found = page.Items.Any(c => c.Id == id);

            found.ShouldBe(status == expected, $"filtering by {status}");
        }
    }

    private async Task PublishExamAsync(string code)
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
            Type = QuestionTypes.SingleChoice,
            Text = code + " question",
            Score = 1m,
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                ],
            }),
        });

        await _exams.PublishAsync(exam.Id);

        _examId = exam.Id;
    }

    private async Task<Guid> PersonAsync(string code) =>
        (await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "On the roll",
            Email = code + "@example.test",
        })).Id;

    private async Task<string> SendAsync(Guid candidateId)
    {
        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = _examId,
            CandidateId = candidateId,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 2,
            SendEmail = false,
        });

        return result.Recipients.Single().Url.Split('/').Last();
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
