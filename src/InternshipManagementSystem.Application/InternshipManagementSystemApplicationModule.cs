using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(InternshipManagementSystemDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(InternshipManagementSystemApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBackgroundWorkersModule)
    )]
public class InternshipManagementSystemApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {

        // Graders are ITransientDependency, so ABP's conventional registration finds
        // every one in the assembly and GraderResolver maps them by type. Adding a
        // question type needs no wiring here — that is the point of the abstraction.
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        // Closes out attempts whose deadline passed while nobody was watching: a
        // closed laptop must not leave an attempt ungraded with its link consumed.
        await context.AddBackgroundWorkerAsync<AttemptTimeoutWorker>();
    }
}
