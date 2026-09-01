using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Settings;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// The defaults an organisation sets for itself, and whether anything reads them.
/// <para>
/// The settings screen describes the pass mark it holds as being <i>applied to
/// any new exam unless its author changes it</i>. It was applied to nothing. The
/// create contract carried a fixed sixty, so there was never a gap for the
/// setting to fill: an organisation that set seventy went on producing exams
/// that passed at sixty, and nothing failed, nothing warned, and the screen went
/// on stating the rule.
/// </para>
/// <para>
/// This is the second setting in that screen found to do nothing. The first —
/// the switch for collecting integrity signals — meant people were watched after
/// being told they were not. A setting written on a screen and read by no code
/// is not a defaulting bug; it is the promise being false.
/// </para>
/// </summary>
public class OrganisationDefaultsTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000081");

    public OrganisationDefaultsTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_new_exam_takes_the_organisations_pass_mark()
    {
        await AsTenantAsync(async () =>
        {
            await GetRequiredService<ISettingManager>().SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.DefaultPassingPercentage, "70");

            var exam = await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "Takes the organisation's default",
                TimeLimitInMinutes = 30,
            });

            exam.PassingPercentage.ShouldBe(70m);
        });
    }

    [Fact]
    public async Task An_author_who_names_a_pass_mark_gets_the_one_they_named()
    {
        await AsTenantAsync(async () =>
        {
            await GetRequiredService<ISettingManager>().SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.DefaultPassingPercentage, "70");

            // "Unless its author changes it" is half the promise, and the half a
            // careless fix breaks: a default that overrides what somebody typed
            // is worse than one that does nothing.
            var exam = await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "Names its own",
                TimeLimitInMinutes = 30,
                PassingPercentage = 45m,
            });

            exam.PassingPercentage.ShouldBe(45m);
        });
    }

    [Fact]
    public async Task An_organisation_that_set_nothing_still_gets_sixty()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "No organisation default",
                TimeLimitInMinutes = 30,
            });

            // Where the number came from in the first place. Removing the fixed
            // sixty from the contract must not leave a zero behind.
            exam.PassingPercentage.ShouldBe(60m);
        });
    }

    [Fact]
    public async Task Editing_an_exam_without_naming_a_pass_mark_leaves_it_alone()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "Set deliberately",
                TimeLimitInMinutes = 30,
                PassingPercentage = 80m,
            });

            await GetRequiredService<ISettingManager>().SetForCurrentTenantAsync(
                InternshipManagementSystemSettings.DefaultPassingPercentage, "70");

            // Reaching for the organisation's default on an edit would move a mark
            // somebody had set on purpose — and on a published exam, move it under
            // candidates who have already sat it.
            var updated = await _exams.UpdateAsync(exam.Id, new CreateUpdateExamDto
            {
                Title = "Set deliberately",
                TimeLimitInMinutes = 30,
            });

            updated.PassingPercentage.ShouldBe(80m);
        });
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
