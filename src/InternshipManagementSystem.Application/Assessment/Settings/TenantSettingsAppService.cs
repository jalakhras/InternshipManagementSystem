using System.Globalization;
using System.Threading.Tasks;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.Settings;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;

namespace InternshipManagementSystem.Assessment.Settings;

/// <summary>
/// What an organisation changes about the platform for itself.
/// <para>
/// The settings were defined and nothing read or wrote them, so every tenant on
/// the deployment saw the same name, the same default language and the same
/// colours. That is the difference between a product several organisations use
/// and a product built for one of them that the others tolerate.
/// </para>
/// </summary>
[Authorize]
public class TenantSettingsAppService : ApplicationService, ITenantSettingsAppService
{
    private readonly ISettingManager _settings;

    public TenantSettingsAppService(ISettingManager settings)
    {
        _settings = settings;
    }

    public async Task<TenantSettingsDto> GetAsync()
    {
        return new TenantSettingsDto
        {
            OrganizationName = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.OrganizationName),
            LogoBlobName = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.LogoBlobName),
            BrandColor = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.BrandColor),
            DefaultLanguage = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.DefaultLanguage),
            TimeZone = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.TimeZone),

            DefaultPassingPercentage = await NumberAsync(
                InternshipManagementSystemSettings.DefaultPassingPercentage, 60m),
            ShowResultToCandidate = await FlagAsync(
                InternshipManagementSystemSettings.ShowResultToCandidate, true),
            CollectIntegritySignals = await FlagAsync(
                InternshipManagementSystemSettings.CollectIntegritySignals, true),
            EnableSelfRegistration = await FlagAsync(
                InternshipManagementSystemSettings.EnableSelfRegistration, false),
        };
    }

    [Authorize(InternshipManagementSystemPermissions.Administration.ManageSettings)]
    public async Task<TenantSettingsDto> UpdateAsync(TenantSettingsDto input)
    {
        // Trimmed to null rather than saved as an empty string: an empty value
        // still overrides the platform default, so clearing the field would leave
        // the header blank rather than falling back.
        await SetAsync(InternshipManagementSystemSettings.OrganizationName, Clean(input.OrganizationName));
        await SetAsync(InternshipManagementSystemSettings.LogoBlobName, Clean(input.LogoBlobName));
        await SetAsync(InternshipManagementSystemSettings.BrandColor, Clean(input.BrandColor));
        await SetAsync(InternshipManagementSystemSettings.DefaultLanguage, Clean(input.DefaultLanguage));
        await SetAsync(InternshipManagementSystemSettings.TimeZone, Clean(input.TimeZone));

        await SetAsync(
            InternshipManagementSystemSettings.DefaultPassingPercentage,
            input.DefaultPassingPercentage.ToString(CultureInfo.InvariantCulture));

        // Invariant, not the current culture. A setting written under an
        // Arabic-Egypt locale and read under an English one has to be the same
        // value, and "True"/"صحيح" is exactly the kind of thing that survives
        // testing and fails in production.
        await SetAsync(
            InternshipManagementSystemSettings.ShowResultToCandidate,
            input.ShowResultToCandidate.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
        await SetAsync(
            InternshipManagementSystemSettings.CollectIntegritySignals,
            input.CollectIntegritySignals.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
        await SetAsync(
            InternshipManagementSystemSettings.EnableSelfRegistration,
            input.EnableSelfRegistration.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

        return await GetAsync();
    }

    private Task SetAsync(string name, string? value) =>
        CurrentTenant.Id is null
            // The host is a tenant too, in the sense that somebody runs the
            // deployment itself and needs a name on it.
            ? _settings.SetGlobalAsync(name, value)
            : _settings.SetForCurrentTenantAsync(name, value);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<decimal> NumberAsync(string name, decimal fallback)
    {
        var raw = await SettingProvider.GetOrNullAsync(name);

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private async Task<bool> FlagAsync(string name, bool fallback)
    {
        var raw = await SettingProvider.GetOrNullAsync(name);

        return bool.TryParse(raw, out var value) ? value : fallback;
    }
}
