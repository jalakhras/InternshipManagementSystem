using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Data;

/* This is used if database provider does't define
 * IInternshipManagementSystemDbSchemaMigrator implementation.
 */
public class NullInternshipManagementSystemDbSchemaMigrator : IInternshipManagementSystemDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
