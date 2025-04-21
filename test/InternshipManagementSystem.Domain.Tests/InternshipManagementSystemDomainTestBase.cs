using Volo.Abp.Modularity;

namespace InternshipManagementSystem;

/* Inherit from this class for your domain layer tests. */
public abstract class InternshipManagementSystemDomainTestBase<TStartupModule> : InternshipManagementSystemTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
