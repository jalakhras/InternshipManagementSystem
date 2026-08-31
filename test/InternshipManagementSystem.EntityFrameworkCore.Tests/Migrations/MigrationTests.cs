using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Migrations;

/// <summary>
/// The migrations, run.
/// <para>
/// Nothing in this repository ran one. The suite builds its schema with
/// <c>IRelationalDatabaseCreator.CreateTables()</c>, which reads the current model
/// and bypasses the migration history entirely — so the model and the migrations
/// could disagree indefinitely, and the disagreement would surface as a host that
/// will not start, or worse, one that starts against a schema missing a column.
/// Every test in the product would still have been green, because every test builds
/// its own database from the model it is testing against.
/// </para>
/// <para>
/// Two different questions are asked here, and only the first can be answered
/// without a database:
/// </para>
/// <para>
/// 1. <em>Do the migrations describe the model?</em> EF Core answers this by
/// comparing the model built from the code against the model recorded in
/// <c>InternshipManagementSystemDbContextModelSnapshot</c>. No connection is
/// opened. This is the check that catches the ordinary mistake — a property added
/// and <c>dotnet ef migrations add</c> not run.
/// </para>
/// <para>
/// 2. <em>Do they actually apply?</em> That needs a server, and it needs the right
/// one: these migrations are written for SQL Server, and applying them to SQLite
/// proves nothing about production even where it succeeds. Those tests are gated on
/// a reachable SQL Server and report themselves skipped when there is none.
/// </para>
/// </summary>
public class MigrationTests
{
    /// <summary>
    /// A context on the SQL Server provider, pointed at a server that need not exist.
    /// <para>
    /// The model, the migrations assembly and the snapshot are all read from the
    /// assembly; a connection is only opened by something that runs SQL.
    /// </para>
    /// </summary>
    private static InternshipManagementSystemDbContext SqlServerContext(string connectionString) =>
        new(new DbContextOptionsBuilder<InternshipManagementSystemDbContext>()
            .UseSqlServer(connectionString)
            .Options);

    [Fact]
    public void The_migrations_and_the_model_agree()
    {
        using var context = SqlServerContext("Server=none;Database=none;Trusted_Connection=True");

        // What `dotnet ef migrations has-pending-model-changes` asks. It fails when
        // an entity or a property was changed and no migration was added for it —
        // which no other test in this repository can see, because every other test
        // builds its tables from the model rather than from the migrations.
        context.Database.HasPendingModelChanges().ShouldBeFalse(
            "the model has changed since the last migration was added. Run "
            + "`dotnet ef migrations add <name>` in src/InternshipManagementSystem.EntityFrameworkCore, "
            + "or the deployed schema will not match the code that queries it.");
    }

    [Fact]
    public void Every_migration_is_a_pair_of_files_with_a_snapshot_beside_them()
    {
        var migrations = MigrationNames();

        // The size of the set before anything is said about its contents. A
        // reflection test that discovered nothing would otherwise assert nothing,
        // and this one is the guard for the two below.
        migrations.Count.ShouldBeGreaterThan(20,
            "far fewer migrations were discovered than this project has; the "
            + "migrations assembly is probably not being read.");

        // Ordered by their timestamp prefix, and unique. A duplicate id makes
        // `Migrate()` non-deterministic about which one it applies.
        migrations.ShouldBeUnique();
        migrations.ShouldBe(migrations.OrderBy(name => name, StringComparer.Ordinal).ToList());
    }

    // ------------------------------------------------- against a real SQL Server

    [SqlServerFact]
    public async Task Every_migration_applies_to_an_empty_sql_server_database()
    {
        await WithThrowawayDatabaseAsync(async context =>
        {
            // The whole point: apply all of them, in order, to nothing — which is
            // what a new deployment does, and what no test has ever done.
            await context.Database.MigrateAsync();

            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();

            applied.ShouldBe(MigrationNames());

            (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        });
    }

    [SqlServerFact]
    public async Task The_schema_the_migrations_build_is_the_schema_the_model_expects()
    {
        await WithThrowawayDatabaseAsync(async context =>
        {
            await context.Database.MigrateAsync();

            // Applying cleanly is not the same as arriving somewhere correct. A
            // migration that creates a column with the wrong type, or that a later
            // one silently failed to alter, applies without complaint and leaves a
            // database the model cannot read.
            context.Database.HasPendingModelChanges().ShouldBeFalse();

            // And the tables are actually there. HasPendingModelChanges compares
            // two models in memory; it would be satisfied by a database with
            // nothing in it at all.
            var tables = await TableNamesAsync(context);

            tables.ShouldContain("AppExams");
            tables.ShouldContain("AppAttempts");
            tables.ShouldContain("AppCandidates");
            tables.ShouldContain("AbpPermissionGrants");
            tables.ShouldContain("__EFMigrationsHistory");
        });
    }

    [SqlServerFact]
    public async Task The_column_the_newest_migration_adds_is_present_and_of_the_type_it_names()
    {
        await WithThrowawayDatabaseAsync(async context =>
        {
            await context.Database.MigrateAsync();

            // Section_On_The_Delivered_Paper, the most recent migration: a nullable
            // uniqueidentifier on AppAttemptQuestions. Named rather than derived, so
            // this fails if the migration is reverted or never runs — which is the
            // specific accident that leaves a host starting against a schema
            // missing a column.
            var column = await ColumnAsync(context, "AppAttemptQuestions", "ExamSectionId");

            column.ShouldNotBeNull("the newest migration did not run, or its column was lost.");
            column!.Value.Type.ShouldBe("uniqueidentifier");
            column.Value.IsNullable.ShouldBeTrue();
        });
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Every migration id in the assembly, in the order EF will apply them.</summary>
    private static List<string> MigrationNames()
    {
        using var context = SqlServerContext("Server=none;Database=none;Trusted_Connection=True");

        return context.GetService<IMigrationsAssembly>()
            .Migrations
            .Keys
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// A database of its own, dropped afterwards whatever happens.
    /// <para>
    /// Named per run so two runs — or a run beside a developer's own database —
    /// cannot collide, and dropped in a <c>finally</c> so a failing assertion does
    /// not leave one behind on somebody's LocalDB.
    /// </para>
    /// </summary>
    private static async Task WithThrowawayDatabaseAsync(Func<InternshipManagementSystemDbContext, Task> action)
    {
        var name = "AstrolabeMigrationCheck_" + Guid.NewGuid().ToString("N");

        await using var context = SqlServerContext(SqlServerTestServer.ForDatabase(name));

        try
        {
            await action(context);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<List<string>> TableNamesAsync(InternshipManagementSystemDbContext context)
    {
        var names = new List<string>();

        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText =
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

        await context.Database.OpenConnectionAsync();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<(string Type, bool IsNullable)?> ColumnAsync(
        InternshipManagementSystemDbContext context, string table, string column)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText =
            "SELECT DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS "
            + "WHERE TABLE_NAME = @table AND COLUMN_NAME = @column";

        command.Parameters.Add(new SqlParameter("@table", table));
        command.Parameters.Add(new SqlParameter("@column", column));

        await context.Database.OpenConnectionAsync();

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return (reader.GetString(0), reader.GetString(1) == "YES");
    }
}
