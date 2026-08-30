using InternshipManagementSystem.Localization;
using InternshipManagementSystem.Settings;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(AbpAuditLoggingDomainSharedModule),
    typeof(AbpBackgroundJobsDomainSharedModule),
    typeof(AbpFeatureManagementDomainSharedModule),
    typeof(AbpIdentityDomainSharedModule),
    typeof(AbpOpenIddictDomainSharedModule),
    typeof(AbpPermissionManagementDomainSharedModule),
    typeof(AbpSettingManagementDomainSharedModule),
    typeof(AbpTenantManagementDomainSharedModule)
    )]
public class InternshipManagementSystemDomainSharedModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        InternshipManagementSystemGlobalFeatureConfigurator.Configure();
        InternshipManagementSystemModuleExtensionConfigurator.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<InternshipManagementSystemDomainSharedModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<InternshipManagementSystemResource>("en")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/InternshipManagementSystem");

            options.DefaultResourceType = typeof(InternshipManagementSystemResource);
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            // "IMS", because that is the prefix the codes actually carry —
            // `IMS:Candidate:EmailTaken`, not `InternshipManagementSystem:…`.
            // ABP keys this on the part before the first colon, so the name here
            // matched nothing and every business error in the product fell back
            // to "an internal error occurred while processing your request".
            //
            // 107 messages were written, translated, and unreachable. A person
            // adding somebody whose address was already on the roll was told the
            // server had broken, rather than that the address was taken — so the
            // one thing they could have fixed themselves was the one thing the
            // product would not tell them.
            options.MapCodeNamespace("IMS", typeof(InternshipManagementSystemResource));
        });

        Configure<AbpSettingOptions>(options =>
        {
            options.DefinitionProviders.Add<InternshipManagementSystemSettingDefinitionProvider>();
        });
    }
}