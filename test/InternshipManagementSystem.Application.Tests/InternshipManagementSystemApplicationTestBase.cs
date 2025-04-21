using Volo.Abp.Modularity;

namespace InternshipManagementSystem;

public abstract class InternshipManagementSystemApplicationTestBase<TStartupModule> : InternshipManagementSystemTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
