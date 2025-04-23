using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace InternshipManagementSystem.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */

public class InternshipManagementSystemDbContextFactory : IDesignTimeDbContextFactory<InternshipManagementSystemDbContext>
{
    public InternshipManagementSystemDbContext CreateDbContext(string[] args)
    {
        InternshipManagementSystemEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<InternshipManagementSystemDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new InternshipManagementSystemDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../InternshipManagementSystem.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}