using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Settings;
using InternshipManagementSystem.Settings;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// An organisation's mark is its own.
/// <para>
/// Every other setting here inherits the host's value, and should: a colour or a
/// default language is a sensible thing to fall back to. A logo is not. It is
/// the name of a file living in the uploading organisation's own blob partition,
/// so an organisation that inherited the host's logo inherited an address it is
/// not allowed to read — and a candidate opening their exam link saw their
/// academy's name beside a broken image.
/// </para>
/// </summary>
public class LogoTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ITenantSettingsAppService _settings;
    private readonly ISettingManager _manager;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000a1");

    public LogoTests()
    {
        _settings = GetRequiredService<ITenantSettingsAppService>();
        _manager = GetRequiredService<ISettingManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_organisation_with_no_logo_does_not_borrow_the_host_s()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            // The host sets one, as a host administrator would.
            await _manager.SetGlobalAsync(
                InternshipManagementSystemSettings.LogoBlobName, "host/a-file-only-the-host-can-read.svg");
        });

        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var mine = await _settings.GetAsync();

                // Nothing rather than somebody else's address. Nothing draws the
                // astrolabe; somebody else's address draws a broken image.
                mine.LogoBlobName.ShouldBeNullOrEmpty();
            });
        }
    }

    [Fact]
    public async Task An_organisation_that_uploads_its_own_logo_keeps_it()
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await _settings.UpdateAsync(new TenantSettingsDto
                {
                    OrganizationName = "أكاديمية المسار",
                    LogoBlobName = "own/mark.png",
                    DefaultPassingPercentage = 50m,
                });
            });

            await WithUnitOfWorkAsync(async () =>
            {
                // The half that keeps the other half honest: refusing to inherit
                // must not turn into refusing to read.
                var mine = await _settings.GetAsync();

                mine.LogoBlobName.ShouldBe("own/mark.png");
            });
        }
    }
}
