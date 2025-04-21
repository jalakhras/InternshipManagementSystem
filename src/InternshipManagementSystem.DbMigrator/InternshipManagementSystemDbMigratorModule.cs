using InternshipManagementSystem.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace InternshipManagementSystem.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(InternshipManagementSystemEntityFrameworkCoreModule),
    typeof(InternshipManagementSystemApplicationContractsModule)
    )]
public class InternshipManagementSystemDbMigratorModule : AbpModule
{
}
