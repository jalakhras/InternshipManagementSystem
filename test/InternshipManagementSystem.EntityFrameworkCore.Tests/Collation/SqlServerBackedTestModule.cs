using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using InternshipManagementSystem.EntityFrameworkCore.Migrations;

namespace InternshipManagementSystem.EntityFrameworkCore.Collation;

/// <summary>
/// The whole application, on the database the product actually ships on.
/// <para>
/// Every other integration test in this repository runs on in-memory SQLite. That
/// is fast and it is right for almost everything — but collation, sort order, case
/// sensitivity and <c>nvarchar(n)</c> caps are not properties of the model, they are
/// properties of the provider, and SQLite's are not SQL Server's. In an Arabic-first
/// product the searches are where that lands: candidate search, results search and
/// question search are all <c>string.Contains</c> over user-entered text.
/// </para>
/// <para>
/// A throwaway database per test, dropped afterwards. Schema comes from
/// <c>EnsureCreated</c> rather than from the migrations, deliberately: whether the
/// migrations apply is a separate question and
/// <see cref="MigrationTests"/> answers it against this same server. Building the
/// schema from the model here keeps these tests about collation and nothing else.
/// </para>
/// </summary>
[DependsOn(
    typeof(InternshipManagementSystemApplicationTestModule),
    typeof(InternshipManagementSystemEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule)
)]
public class SqlServerBackedTestModule : AbpModule
{
    private string _connectionString = string.Empty;

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FeatureManagementOptions>(options =>
        {
            options.SaveStaticFeaturesToDatabase = false;
            options.IsDynamicFeatureStoreEnabled = false;
        });
        Configure<PermissionManagementOptions>(options =>
        {
            options.SaveStaticPermissionsToDatabase = false;
            options.IsDynamicPermissionStoreEnabled = false;
        });
        Configure<SettingManagementOptions>(options =>
        {
            options.SaveStaticSettingsToDatabase = false;
            options.IsDynamicSettingStoreEnabled = false;
        });

        _connectionString = SqlServerTestServer.ForDatabase(
            "AstrolabeCollation_" + Guid.NewGuid().ToString("N"));

        using (var creator = Context(_connectionString))
        {
            creator.Database.EnsureCreated();
        }

        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(configuration => configuration.DbContextOptions.UseSqlServer(_connectionString));
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        // Pooled connections keep the database in use and the drop fails silently
        // otherwise, leaving one behind on somebody's LocalDB per test run.
        SqlConnection.ClearAllPools();

        using var creator = Context(_connectionString);

        creator.Database.EnsureDeleted();
    }

    private static InternshipManagementSystemDbContext Context(string connectionString) =>
        new(new DbContextOptionsBuilder<InternshipManagementSystemDbContext>()
            .UseSqlServer(connectionString)
            .Options);
}
