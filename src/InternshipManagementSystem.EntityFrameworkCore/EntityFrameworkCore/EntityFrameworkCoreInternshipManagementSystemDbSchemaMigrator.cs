using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InternshipManagementSystem.Data;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.EntityFrameworkCore;

public class EntityFrameworkCoreInternshipManagementSystemDbSchemaMigrator
    : IInternshipManagementSystemDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreInternshipManagementSystemDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the InternshipManagementSystemDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<InternshipManagementSystemDbContext>()
            .Database
            .MigrateAsync();
    }
}
