using InternshipManagementSystem.TrainingManagement.Grading;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
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
    typeof(AbpSettingManagementApplicationModule)
    )]
public class InternshipManagementSystemApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<InternshipManagementSystemApplicationModule>();
        });
        var services = context.Services;

        services.AddSingleton<ICandidateExamGradingService, CandidateExamGradingService>();

    }
}