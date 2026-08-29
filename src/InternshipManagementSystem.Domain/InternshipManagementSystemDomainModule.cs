using InternshipManagementSystem.MultiTenancy;
using InternshipManagementSystem.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using System.IO;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Emailing;
using Volo.Abp.MailKit;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.PermissionManagement.OpenIddict;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(InternshipManagementSystemDomainSharedModule),
    typeof(AbpAuditLoggingDomainModule),
    typeof(AbpBackgroundJobsDomainModule),
    typeof(AbpFeatureManagementDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpOpenIddictDomainModule),
    typeof(AbpPermissionManagementDomainOpenIddictModule),
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpTenantManagementDomainModule),
    typeof(AbpEmailingModule),
    typeof(AbpMailKitModule),
    typeof(AbpBlobStoringModule),
    typeof(AbpBlobStoringFileSystemModule)
)]
public class InternshipManagementSystemDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            // Arabic first, and first deliberately: ABP falls back to the head of
            // this list when a request expresses no preference, so the ordering here
            // is what makes Arabic the default rather than a special case elsewhere.
            // A tenant or a user can switch to English from settings at any time.
            options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));

            // The template offered eighteen languages we do not translate or test.
            // A half-translated screen reads as broken, and RTL correctness has to be
            // verified per language rather than assumed.
        });

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });

        Configure<AbpDataSeedOptions>(options =>
        {
            options.Contributors.Add<InternshipManagementSystemDataSeedContributor>();
        });
        Configure<AbpSettingOptions>(options =>
        {
            options.DefinitionProviders.Add<InternshipManagementSystemSettingDefinitionProvider>();
        });

        // Somewhere for uploaded media to actually go.
        //
        // The container and the service that writes to it were both finished and no
        // provider was ever configured, so every upload and every read threw at
        // activation: "No BLOB Storage provider was used". Nothing in the test suite
        // saw it, because the tests substitute the container.
        //
        // The filesystem provider is the right default for a single-server
        // deployment and for running this locally. Moving to S3 or Azure later is a
        // configuration change here, not a rewrite: the container name is a
        // constant and blob names are generated.
        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.ConfigureDefault(container =>
            {
                container.UseFileSystem(fileSystem =>
                {
                    // Under the content root rather than the working directory. The
                    // old upload path read its root from a config key set to an
                    // empty string, and an empty string is not null — so the
                    // fallback never fired and files landed wherever the process
                    // happened to start.
                    fileSystem.BasePath = Path.Combine(
                        Directory.GetCurrentDirectory(), "App_Data", "blobs");
                });
            });
        });

#if DEBUG
        context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
#endif
    }
}