using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpBackgroundJobsAbstractionsModule)
    )]
public class InternshipManagementSystemTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Settings the delivery path refuses to start without. The signing key has
        // no fallback by design: an exam session token is what lets a candidate
        // reach their attempt, so a default value would be one shared secret across
        // every deployment. Tests supply one like anybody else, and this one is
        // deliberately obvious so nobody copies it into a real appsettings.
        context.Services.ReplaceConfiguration(
            new ConfigurationBuilder()
                .AddConfiguration(context.Services.GetConfiguration())
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ExamSession:SigningKey"] = "test-only-signing-key-not-for-any-real-deployment",
                    ["App:ClientUrl"] = "https://localhost:4200",
                })
                .Build());

        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
        });

        // Copied aside first: AddAlwaysAllowAuthorization does not add alongside the
        // real authorisation services, it replaces them outright, and a test module
        // that wants [Authorize] actually executed has no way back to them once they
        // are gone. See RealAuthorizationServices, and PermissionEnforcedTestModule
        // which uses it. Nothing about this line changes how any existing test runs.
        Security.RealAuthorizationServices.Capture(context);

        context.Services.AddAlwaysAllowAuthorization();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        SeedTestData(context);
    }

    private static void SeedTestData(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () =>
        {
            using (var scope = context.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider
                    .GetRequiredService<IDataSeeder>()
                    .SeedAsync();
            }
        });
    }
}