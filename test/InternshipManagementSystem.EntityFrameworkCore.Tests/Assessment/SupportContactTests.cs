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
using InternshipManagementSystem.Assessment.Settings;
using InternshipManagementSystem.Settings;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Somewhere for a candidate to write when something goes wrong.
/// <para>
/// A connection drops twenty minutes into a paper. A recording will not start.
/// A question will not render. The only address anywhere on the candidate's
/// screen was ours — and they have no relationship with this platform at all:
/// the centre invited them, the centre set the clock, and the centre is the
/// only party that can extend a link or let them sit again.
/// </para>
/// <para>
/// It read like a small thing on the board and it is not. It is the difference
/// between a candidate who lost their sitting and a candidate who lost their
/// sitting and had no way to say so.
/// </para>
/// </summary>
public class SupportContactTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ITenantSettingsAppService _settings;
    private readonly ISettingManager _manager;
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000d5");
    private static readonly Guid Other = Guid.Parse("cccccccc-0000-0000-0000-0000000000d6");

    public SupportContactTests()
    {
        _settings = GetRequiredService<ITenantSettingsAppService>();
        _manager = GetRequiredService<ISettingManager>();
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_organisation_can_say_where_its_candidates_should_write()
    {
        await AsTenantAsync(Tenant, async () =>
        {
            await _settings.UpdateAsync(new TenantSettingsDto
            {
                OrganizationName = "مركز الرياض للّغات",
                SupportEmail = "help@riyadh-languages.test",
                DefaultPassingPercentage = 60m,
                ShowResultToCandidate = true,
            });

            (await _settings.GetAsync()).SupportEmail.ShouldBe("help@riyadh-languages.test");
        });
    }

    [Fact]
    public async Task The_candidate_is_given_the_centres_address_not_ours()
    {
        // The host publishes one, as whoever runs the deployment would.
        await WithUnitOfWorkAsync(async () =>
            await _manager.SetGlobalAsync(
                InternshipManagementSystemSettings.SupportEmail, "support@astrolabe.test"));

        await AsTenantAsync(Tenant, async () =>
            await _manager.SetForTenantAsync(
                Tenant, InternshipManagementSystemSettings.SupportEmail, "help@centre.test"));

        var preview = await OpenAsync(Tenant, "support-a");

        // Read tenant-only, for the reason the logo is. A candidate handed the
        // host's address is handed ours, and writing to us about a paper we do
        // not run is a message nobody can answer.
        preview.OrganizationSupportEmail.ShouldBe("help@centre.test");
    }

    [Fact]
    public async Task An_organisation_that_published_no_address_borrows_nobody_elses()
    {
        await WithUnitOfWorkAsync(async () =>
            await _manager.SetGlobalAsync(
                InternshipManagementSystemSettings.SupportEmail, "support@astrolabe.test"));

        var preview = await OpenAsync(Other, "support-b");

        // Nothing rather than ours. An organisation that would rather not publish
        // an address to candidates has said so by leaving it empty, and handing
        // out the platform's instead overrules them — and sends their candidates
        // to a party that cannot help.
        preview.OrganizationSupportEmail.ShouldBeNullOrEmpty();
    }

    // ------------------------------------------------------------------ helpers

    private async Task<ExamPreviewDto> OpenAsync(Guid tenantId, string code)
    {
        string token = null!;

        await AsTenantAsync(tenantId, async () =>
        {
            var categories = GetRequiredService<IRepository<Category, Guid>>();
            var category = await categories.InsertAsync(
                new Category(Guid.NewGuid(), tenantId, code, code), autoSave: true);

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
                Text = code + " — write something",
                Score = 10m,
                Payload = PayloadJson.Write(new RubricPayload()),
            });

            await _exams.PublishAsync(exam.Id);

            var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Lost their connection",
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

            token = sent.Recipients.Single().Url.Split('/').Last();
        });

        // Opened outside the tenant, the way a candidate does it: they have no
        // account and no organisation of their own, and the link is the whole
        // credential.
        return await WithUnitOfWorkAsync(async () => await _taking.OpenLinkAsync(token));
    }

    private async Task AsTenantAsync(Guid tenantId, Func<Task> action)
    {
        using (_currentTenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
