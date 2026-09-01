using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace InternshipManagementSystem.Data;

public class InternshipManagementSystemDbMigrationService : ITransientDependency
{
    public ILogger<InternshipManagementSystemDbMigrationService> Logger { get; set; }

    private readonly IDataSeeder _dataSeeder;
    private readonly IEnumerable<IInternshipManagementSystemDbSchemaMigrator> _dbSchemaMigrators;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IdentityUserManager _userManager;

    public InternshipManagementSystemDbMigrationService(
        IDataSeeder dataSeeder,
        IEnumerable<IInternshipManagementSystemDbSchemaMigrator> dbSchemaMigrators,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        IdentityUserManager userManager)
    {
        _dataSeeder = dataSeeder;
        _dbSchemaMigrators = dbSchemaMigrators;
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
        _userManager = userManager;

        Logger = NullLogger<InternshipManagementSystemDbMigrationService>.Instance;
    }

    public async Task MigrateAsync()
    {
        var initialMigrationAdded = AddInitialMigrationIfNotExist();

        if (initialMigrationAdded)
        {
            return;
        }

        Logger.LogInformation("Started database migrations...");

        await MigrateDatabaseSchemaAsync();
        await SeedDataAsync();

        Logger.LogInformation($"Successfully completed host database migrations.");

        var tenants = await _tenantRepository.GetListAsync(includeDetails: true);

        var migratedDatabaseSchemas = new HashSet<string>();
        foreach (var tenant in tenants)
        {
            using (_currentTenant.Change(tenant.Id))
            {
                if (tenant.ConnectionStrings.Any())
                {
                    var tenantConnectionStrings = tenant.ConnectionStrings
                        .Select(x => x.Value)
                        .ToList();

                    if (!migratedDatabaseSchemas.IsSupersetOf(tenantConnectionStrings))
                    {
                        await MigrateDatabaseSchemaAsync(tenant);

                        migratedDatabaseSchemas.AddIfNotContains(tenantConnectionStrings);
                    }
                }

                await SeedDataAsync(tenant);
            }

            Logger.LogInformation($"Successfully completed {tenant.Name} tenant database migrations.");
        }

        Logger.LogInformation("Successfully completed all database migrations.");
        Logger.LogInformation("You can safely end this process...");
    }

    private async Task MigrateDatabaseSchemaAsync(Tenant? tenant = null)
    {
        Logger.LogInformation(
            $"Migrating schema for {(tenant == null ? "host" : tenant.Name + " tenant")} database...");

        foreach (var migrator in _dbSchemaMigrators)
        {
            await migrator.MigrateAsync();
        }
    }

    private async Task SeedDataAsync(Tenant? tenant = null)
    {
        Logger.LogInformation($"Executing {(tenant == null ? "host" : tenant.Name + " tenant")} database seed...");

        await _dataSeeder.SeedAsync(new DataSeedContext(tenant?.Id)
            .WithProperty(IdentityDataSeedContributor.AdminEmailPropertyName, IdentityDataSeedContributor.AdminEmailDefaultValue)
            .WithProperty(IdentityDataSeedContributor.AdminPasswordPropertyName, IdentityDataSeedContributor.AdminPasswordDefaultValue)
        );

        if (tenant is null)
        {
            await NameTheHostAdministratorAsync();
        }
    }

    /// <summary>The name the platform operator signs in with.</summary>
    private const string HostAdministratorUserName = "jassar";

    /// <summary>
    /// Gives the host administrator its name, every time the seed runs.
    /// <para>
    /// ABP's identity seeder creates the account as <c>admin</c> and looks for
    /// that exact name to decide whether it already exists. So renaming the row
    /// by hand does not hold: the next seed finds no <c>admin</c>, concludes
    /// there is no administrator, and makes a second one — leaving two accounts
    /// with full control of the platform, one of which nobody meant to have.
    /// </para>
    /// <para>
    /// Renaming here instead, after the seeder has run and on every run, makes
    /// the name a property of this deployment rather than a change somebody has
    /// to remember to repeat. Idempotent by construction: if the account already
    /// carries the name there is nothing to do, and if both names somehow exist
    /// the seeded one is left alone rather than merged, because merging two
    /// administrators is not something a migration should decide.
    /// </para>
    /// </summary>
    private async Task NameTheHostAdministratorAsync()
    {
        if (await _userManager.FindByNameAsync(HostAdministratorUserName) is not null)
        {
            return;
        }

        var seeded = await _userManager.FindByNameAsync("admin");

        if (seeded is null)
        {
            return;
        }

        (await _userManager.SetUserNameAsync(seeded, HostAdministratorUserName)).CheckErrors();

        Logger.LogInformation(
            "The host administrator signs in as {UserName}.", HostAdministratorUserName);
    }

    private bool AddInitialMigrationIfNotExist()
    {
        try
        {
            if (!DbMigrationsProjectExists())
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        try
        {
            if (!MigrationsFolderExists())
            {
                AddInitialMigration();
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning("Couldn't determinate if any migrations exist : " + e.Message);
            return false;
        }
    }

    private bool DbMigrationsProjectExists()
    {
        var dbMigrationsProjectFolder = GetEntityFrameworkCoreProjectFolderPath();

        return dbMigrationsProjectFolder != null;
    }

    private bool MigrationsFolderExists()
    {
        var dbMigrationsProjectFolder = GetEntityFrameworkCoreProjectFolderPath();
        return dbMigrationsProjectFolder != null && Directory.Exists(Path.Combine(dbMigrationsProjectFolder, "Migrations"));
    }

    private void AddInitialMigration()
    {
        Logger.LogInformation("Creating initial migration...");

        string argumentPrefix;
        string fileName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            argumentPrefix = "-c";
            fileName = "/bin/bash";
        }
        else
        {
            argumentPrefix = "/C";
            fileName = "cmd.exe";
        }

        var procStartInfo = new ProcessStartInfo(fileName,
            $"{argumentPrefix} \"abp create-migration-and-run-migrator \"{GetEntityFrameworkCoreProjectFolderPath()}\"\""
        );

        try
        {
            Process.Start(procStartInfo);
        }
        catch (Exception)
        {
            throw new Exception("Couldn't run ABP CLI...");
        }
    }

    private string? GetEntityFrameworkCoreProjectFolderPath()
    {
        var slnDirectoryPath = GetSolutionDirectoryPath();

        if (slnDirectoryPath == null)
        {
            throw new Exception("Solution folder not found!");
        }

        var srcDirectoryPath = Path.Combine(slnDirectoryPath, "src");

        return Directory.GetDirectories(srcDirectoryPath)
            .FirstOrDefault(d => d.EndsWith(".EntityFrameworkCore"));
    }

    private string? GetSolutionDirectoryPath()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory != null && Directory.GetParent(currentDirectory.FullName) != null)
        {
            currentDirectory = Directory.GetParent(currentDirectory.FullName);

            if (currentDirectory != null && Directory.GetFiles(currentDirectory.FullName).FirstOrDefault(f => f.EndsWith(".sln")) != null)
            {
                return currentDirectory.FullName;
            }
        }

        return null;
    }
}