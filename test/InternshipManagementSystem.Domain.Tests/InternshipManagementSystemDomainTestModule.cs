using Volo.Abp.Modularity;

namespace InternshipManagementSystem;

[DependsOn(
    typeof(InternshipManagementSystemDomainModule),
    typeof(InternshipManagementSystemTestBaseModule)
)]
public class InternshipManagementSystemDomainTestModule : AbpModule
{

}
