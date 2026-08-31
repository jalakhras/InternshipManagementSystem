using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People;
using Candidate = InternshipManagementSystem.Assessment.People.Candidate;
using InternshipManagementSystem.Assessment.People.Dtos;
using InternshipManagementSystem.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Collation;

/// <summary>
/// The same six searches, run twice: once on the database the tests use, once on the
/// database the product ships on.
/// <para>
/// Candidate search, results search and question search are all
/// <c>string.Contains</c> over user-entered text. EF Core turns that into
/// <c>instr()</c> on SQLite — a byte-exact comparison — and into a collation-aware
/// <c>LIKE</c>/<c>CHARINDEX</c> on SQL Server, where the answer depends on the
/// database's collation. The two do not agree, and until now every assertion this
/// repository made about a search ran only on the one the product does not use.
/// </para>
/// <para>
/// The pairs below are deliberately written as two classes with the same test names
/// and different expected values, rather than as one parameterised test: the point
/// is the disagreement, and a shared expectation would hide it. Where the two agree
/// — tashkeel, hamza — that is a finding too, and a worse one: it is the product
/// failing an Arabic reader identically everywhere.
/// </para>
/// </summary>
public static class ArabicSearchFixture
{
    public static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000c1");

    /// <summary>
    /// A name with capitals in it, which people retype in lower case when searching.
    /// <para>
    /// A name and not an email: <c>CandidateAppService</c> lower-cases every email
    /// before storing it, so email search is already immune to this. Names are not
    /// normalised, and neither is question text or an exam title — the other two
    /// searches this product runs.
    /// </para>
    /// </summary>
    public const string MixedCaseName = "Ahmed Saleh";

    /// <summary>A name written with full tashkeel, as a careful registrar would type it.</summary>
    public const string NameWithTashkeel = "مُحَمَّد عبد الله";

    /// <summary>The same name as everybody else types it.</summary>
    public const string NameWithoutTashkeel = "محمد";

    /// <summary>Written with hamza; searched for without, which is what people do.</summary>
    public const string NameWithHamza = "أحمد سالم";

    /// <summary>Six names whose order is the whole question.</summary>
    public static readonly string[] SortingNames =
        ["احمد", "أحمد", "إبراهيم", "ابراهيم", "آمنة", "بدر"];
}

/// <summary>The searches as every existing test in this repository sees them.</summary>
public class ArabicSearchOnSqliteTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICandidateAppService _candidates;
    private readonly ICurrentTenant _currentTenant;

    public ArabicSearchOnSqliteTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_name_typed_in_lower_case_finds_the_person_registered_with_capitals()
    {
        await AsTenantAsync(async () =>
        {
            await SeedAsync(_candidates, ArabicSearchFixture.MixedCaseName, "case@example.test");

            var found = await SearchAsync(_candidates, "ahmed saleh");

            // Found — and it was not, when this test was written.
            //
            // EF Core compiles Contains to instr() here, which compares code
            // units, so the raw comparison still misses. What answers now is the
            // folded copy of the name, stored lower-cased: the two providers
            // agreed on nothing here and now agree, which is the point of
            // folding rather than leaning on a collation.
            found.ShouldNotBeEmpty();

            // The control: the exact spelling is found, so the fixture is real and
            // the emptiness above is about case and nothing else.
            (await SearchAsync(_candidates, "Ahmed Saleh")).ShouldHaveSingleItem();
        });
    }

    [Fact]
    public async Task A_name_written_with_tashkeel_is_found_by_its_plain_spelling()
    {
        await AsTenantAsync(async () =>
        {
            await SeedAsync(_candidates, ArabicSearchFixture.NameWithTashkeel, "tashkeel@example.test");

            // Found on both providers now. It was found on neither, and no
            // collation could have changed that: LIKE matches positionally, and
            // the tashkeel are characters sitting between the letters. It needed
            // a folded column, and it has one.
            (await SearchAsync(_candidates, ArabicSearchFixture.NameWithoutTashkeel)).ShouldNotBeEmpty();

            (await SearchAsync(_candidates, ArabicSearchFixture.NameWithTashkeel)).ShouldHaveSingleItem();
        });
    }

    [Fact]
    public async Task Names_sort_by_code_point()
    {
        await AsTenantAsync(async () =>
        {
            foreach (var name in ArabicSearchFixture.SortingNames)
            {
                await SeedAsync(_candidates, name, Guid.NewGuid().ToString("N") + "@example.test");
            }

            var ordered = (await SearchAsync(_candidates, null)).Select(c => c.FullName).ToList();

            // UTF-16 order: آ U+0622, أ U+0623, إ U+0625, ا U+0627, ب U+0628. The
            // four alef spellings scatter, so إبراهيم and ابراهيم — the same name —
            // land two apart with أحمد between them.
            ordered.ShouldBe(["آمنة", "أحمد", "إبراهيم", "ابراهيم", "احمد", "بدر"]);
        });
    }

    [Fact]
    public async Task A_name_longer_than_the_column_is_stored_without_complaint()
    {
        await AsTenantAsync(async () =>
        {
            var repository = GetRequiredService<IRepository<Candidate, Guid>>();

            var tooLong = new string('م', 300);

            await repository.InsertAsync(
                new Candidate(
                    Guid.NewGuid(), ArabicSearchFixture.Tenant, tooLong, "long@example.test"),
                autoSave: true);

            // SQLite has no length constraint at all: nvarchar(256) is a type name
            // it records and does not enforce. The same insert is an error on SQL
            // Server, so a validation gap that reaches the database is invisible
            // to every test in this repository.
            var stored = await repository.FirstOrDefaultAsync(c => c.Email == "long@example.test");

            stored.ShouldNotBeNull();
            stored!.FullName.Length.ShouldBe(300);
        });
    }

    private Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(ArabicSearchFixture.Tenant))
        {
            return WithUnitOfWorkAsync(action);
        }
    }

    internal static async Task SeedAsync(ICandidateAppService candidates, string name, string email) =>
        await candidates.CreateAsync(new CreateUpdateCandidateDto { FullName = name, Email = email });

    internal static async Task<List<CandidateDto>> SearchAsync(ICandidateAppService candidates, string? filter) =>
        (await candidates.GetListAsync(new CandidateListRequestDto { Filter = filter, MaxResultCount = 50 }))
        .Items.ToList();
}

/// <summary>The same searches on SQL Server, which is what a user actually meets.</summary>
public class ArabicSearchOnSqlServerTests : InternshipManagementSystemTestBase<SqlServerBackedTestModule>
{
    private readonly ICandidateAppService _candidates;
    private readonly ICurrentTenant _currentTenant;

    public ArabicSearchOnSqlServerTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [SqlServerFact]
    public async Task A_name_typed_in_lower_case_finds_the_person_registered_with_capitals()
    {
        await AsTenantAsync(async () =>
        {
            await ArabicSearchOnSqliteTests.SeedAsync(
                _candidates, ArabicSearchFixture.MixedCaseName, "case@example.test");

            // Found, because the column's collation is case-insensitive
            // (SQL_Latin1_General_CP1_CI_AS on this server, and CI on any ordinary
            // one). The SQLite test of the same shape asserts the opposite, and
            // both are true of the database they run on. This is the divergence in
            // its plainest form.
            (await ArabicSearchOnSqliteTests.SearchAsync(_candidates, "ahmed saleh"))
                .ShouldHaveSingleItem();
        });
    }

    [SqlServerFact]
    public async Task A_name_written_with_tashkeel_is_found_by_its_plain_spelling_here_too()
    {
        await AsTenantAsync(async () =>
        {
            await ArabicSearchOnSqliteTests.SeedAsync(
                _candidates, ArabicSearchFixture.NameWithTashkeel, "tashkeel@example.test");

            // The two providers agree, and the agreement is the bad news: a
            // registrar who types مُحَمَّد cannot be found by anybody searching محمد,
            // in an Arabic-first product. No collation fixes this — LIKE matches
            // positionally, and the tashkeel are characters sitting between the
            // letters, so even Arabic_CI_AI answers no. It needs a normalised
            // search column, not a collation change.
            (await ArabicSearchOnSqliteTests.SearchAsync(
                _candidates, ArabicSearchFixture.NameWithoutTashkeel)).ShouldNotBeEmpty();

            (await ArabicSearchOnSqliteTests.SearchAsync(
                _candidates, ArabicSearchFixture.NameWithTashkeel)).ShouldHaveSingleItem();
        });
    }

    [SqlServerFact]
    public async Task Names_sort_by_collation_and_not_by_code_point()
    {
        await AsTenantAsync(async () =>
        {
            foreach (var name in ArabicSearchFixture.SortingNames)
            {
                await ArabicSearchOnSqliteTests.SeedAsync(
                    _candidates, name, Guid.NewGuid().ToString("N") + "@example.test");
            }

            var ordered = (await ArabicSearchOnSqliteTests.SearchAsync(_candidates, null))
                .Select(c => c.FullName).ToList();

            // A different order from SQLite's, on the same six names. Any test that
            // asserts a sorted roster of Arabic names on SQLite is asserting an
            // order production does not produce.
            ordered.ShouldBe(["إبراهيم", "أحمد", "آمنة", "ابراهيم", "احمد", "بدر"]);
        });
    }

    [SqlServerFact]
    public async Task A_name_longer_than_the_column_is_refused()
    {
        var repository = GetRequiredService<IRepository<Candidate, Guid>>();

        var tooLong = new string('م', 300);

        // The assertion wraps the whole unit of work, not just the insert. A failed
        // save leaves the entity in the change tracker, so completing the unit of
        // work afterwards raises the same error a second time — asserting only
        // around the insert catches the first and is then killed by the second.
        using (_currentTenant.Change(ArabicSearchFixture.Tenant))
        {
            // Refused by the database. The SQLite test of the same name stores it
            // happily, so nvarchar(256) is enforced in production and nowhere in
            // the suite: an input path that fails to cap a name ships green and
            // errors live.
            await Should.ThrowAsync<DbUpdateException>(async () =>
                await WithUnitOfWorkAsync(async () =>
                    await repository.InsertAsync(
                        new Candidate(
                            Guid.NewGuid(), ArabicSearchFixture.Tenant, tooLong, "long@example.test"),
                        autoSave: true)));
        }
    }

    private Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(ArabicSearchFixture.Tenant))
        {
            return WithUnitOfWorkAsync(action);
        }
    }
}
