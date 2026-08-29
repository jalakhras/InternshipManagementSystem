using InternshipManagementSystem.Localization;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace InternshipManagementSystem.Settings;

public class InternshipManagementSystemSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            // Arabic is the platform default. Everything here is per-tenant, so an
            // English-speaking tenant changes its own without affecting anyone else.
            Tenant(InternshipManagementSystemSettings.DefaultLanguage, "ar"),
            Tenant(InternshipManagementSystemSettings.TimeZone, "Asia/Riyadh"),
            // Bookkeeping, never shown: not client-visible and not per-tenant,
            // because it records what this deployment's seeder has done.
            new SettingDefinition(InternshipManagementSystemSettings.SeededPermissions)
                .WithProviders(GlobalSettingValueProvider.ProviderName),

            Tenant(InternshipManagementSystemSettings.OrganizationName, null),
            Tenant(InternshipManagementSystemSettings.LogoBlobName, null),
            Tenant(InternshipManagementSystemSettings.BrandColor, null),
            Tenant(InternshipManagementSystemSettings.DefaultPassingPercentage, "60"),
            Tenant(InternshipManagementSystemSettings.ShowResultToCandidate, "true"),
            Tenant(InternshipManagementSystemSettings.CollectIntegritySignals, "true"),
            Tenant(InternshipManagementSystemSettings.EnableSelfRegistration, "false")
        );
    }

    /// <summary>
    /// A tenant-scoped setting, visible to clients so the Angular shell can read
    /// language, branding and timezone before it renders anything.
    /// </summary>
    private static SettingDefinition Tenant(string name, string? defaultValue) =>
        new SettingDefinition(
                name,
                defaultValue,
                L($"DisplayName:{name}"),
                L($"Description:{name}"),
                isVisibleToClients: true)
            .WithProviders(
                TenantSettingValueProvider.ProviderName,
                GlobalSettingValueProvider.ProviderName);

    private static LocalizableString L(string name) =>
        LocalizableString.Create<InternshipManagementSystemResource>(name);
}
