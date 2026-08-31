using InternshipManagementSystem.MultiTenancy;
using InternshipManagementSystem.Settings;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
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
        var configuration = context.Services.GetConfiguration();

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
                    fileSystem.BasePath = ResolveBlobBasePath(configuration);
                });
            });
        });

        // Whether to send mail at all.
        //
        // Unconfigured, this is what it has always been: a null sender in DEBUG and a
        // real SMTP client otherwise. A container built in Release and deployed before
        // a relay exists wants the null sender too — otherwise every invitation spends
        // a connect timeout failing to reach 127.0.0.1:25, and a bulk assignment to a
        // group of forty stalls for minutes before reporting what it already knew.
        if (configuration.GetValue<bool?>("Mailing:UseNullSender") ?? IsDebugBuild)
        {
            context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
        }

        ConfigureTransportSecurity(context, configuration);
    }

    /// <summary>
    /// How the connection to the relay is secured. The rule itself lives in
    /// <see cref="Assessment.Delivery.MailTransport"/>, where a test can hold it:
    /// this one was found by sending a real message and reading a server log,
    /// which is not a way to find it twice.
    /// </summary>
    private void ConfigureTransportSecurity(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        var port = configuration.GetValue<int?>("Settings:Abp.Mailing.Smtp.Port") ?? 25;
        var ssl = configuration.GetValue<bool?>("Settings:Abp.Mailing.Smtp.EnableSsl") ?? false;

        Configure<AbpMailKitOptions>(options =>
        {
            options.SecureSocketOption = Assessment.Delivery.MailTransport.SecurityFor(port, ssl);
        });
    }

    /// <summary>
    /// Where uploaded question media and candidate answer files live.
    /// <para>
    /// Configurable because in a container this has to be a mounted volume: the
    /// default sits in the writable layer, which is discarded when the container is
    /// replaced, and a redeploy would take every uploaded file with it. The default
    /// is the path this has always used, so a developer sees no change.
    /// </para>
    /// <para>
    /// Relative to the working directory rather than the content root, kept from the
    /// original: the old upload path read its root from a config key set to an empty
    /// string, and an empty string is not null — so the fallback never fired and files
    /// landed wherever the process happened to start. Hence the blank check here.
    /// </para>
    /// </summary>
    private static string ResolveBlobBasePath(IConfiguration configuration)
    {
        var configured = configuration["BlobStoring:FileSystem:BasePath"];

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "blobs")
            : Path.GetFullPath(configured, Directory.GetCurrentDirectory());
    }

    private static bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif
}