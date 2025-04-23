using Volo.Abp.Modularity;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(InternshipManagementSystemApplicationModule),
    typeof(InternshipManagementSystemDomainTestModule)
)]
public class InternshipManagementSystemApplicationTestModule : AbpModule
{
}