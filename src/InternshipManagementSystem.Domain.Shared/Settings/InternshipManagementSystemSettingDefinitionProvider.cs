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
            // Bookkeeping, never shown. Per tenant, not global: the grants it
            // records are tenant-scoped, and one value for the whole deployment
            // meant the host's pass marked the work done for every tenant that
            // followed.
            new SettingDefinition(InternshipManagementSystemSettings.SeededPermissions)
                .WithProviders(
                    TenantSettingValueProvider.ProviderName,
                    GlobalSettingValueProvider.ProviderName),

            // The same bookkeeping for the coordinator, author, marker and
            // observer roles, and per tenant for the same reason.
            new SettingDefinition(InternshipManagementSystemSettings.SeededRolePermissions)
                .WithProviders(
                    TenantSettingValueProvider.ProviderName,
                    GlobalSettingValueProvider.ProviderName),

            Tenant(InternshipManagementSystemSettings.OrganizationName, null),
            Tenant(InternshipManagementSystemSettings.LogoBlobName, null),
            Tenant(InternshipManagementSystemSettings.BrandColor, null),
            Tenant(InternshipManagementSystemSettings.SupportEmail, null),
            Tenant(InternshipManagementSystemSettings.DefaultPassingPercentage, "60"),
            Tenant(InternshipManagementSystemSettings.ShowResultToCandidate, "true"),
            Tenant(InternshipManagementSystemSettings.CollectIntegritySignals, "true")
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
