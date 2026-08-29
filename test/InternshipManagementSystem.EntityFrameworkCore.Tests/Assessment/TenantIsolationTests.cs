using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// The exit gate for the multi-tenant rebuild.
/// <para>
/// Multi-tenancy was switched on in this codebase from the start, but no business
/// entity implemented <c>IMultiTenant</c>. ABP's tenant filter only applies to
/// entities that do, which produced the most dangerous shape available: users were
/// separated while their data was not. A trading academy logged into its own account
/// would have read a language school's entire question bank, candidate list and
/// results.
/// </para>
/// <para>
/// These tests write as one tenant and read as another. They are deliberately
/// end-to-end through the real repositories and the real DbContext, because the
/// property under test is "the filter is actually attached", which a unit test
/// against the entities could not tell you.
/// </para>
/// </summary>
public class TenantIsolationTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public TenantIsolationTests()
    {
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_exams()
    {
        var repo = GetRequiredService<IRepository<Exam, Guid>>();

        await AsTenantAsync(TenantA, async () =>
        {
            await repo.InsertAsync(new Exam(Guid.NewGuid(), TenantA, "Spanish B1 Placement", 60), autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            var visible = await repo.GetListAsync();
            visible.ShouldBeEmpty();
        });

        await AsTenantAsync(TenantA, async () =>
        {
            var visible = await repo.GetListAsync();
            visible.Count.ShouldBe(1);
            visible[0].Title.ShouldBe("Spanish B1 Placement");
        });
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_question_bank()
    {
        // The question bank holds the answer keys, so this is the leak that would
        // have mattered most commercially.
        var exams = GetRequiredService<IRepository<Exam, Guid>>();
        var repo = GetRequiredService<IRepository<Question, Guid>>();
        var examId = Guid.NewGuid();

        await AsTenantAsync(TenantA, async () =>
        {
            await exams.InsertAsync(new Exam(examId, TenantA, "Technical Analysis", 45), autoSave: true);

            await repo.InsertAsync(
                new Question(Guid.NewGuid(), TenantA, examId, QuestionTypes.SingleChoice, "Which is a support level?")
                {
                    Payload = """{"options":[{"id":"a","text":"1.0820","isCorrect":true}]}"""
                },
                autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            (await repo.GetListAsync()).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_candidates()
    {
        var repo = GetRequiredService<IRepository<Candidate, Guid>>();

        await AsTenantAsync(TenantA, async () =>
        {
            await repo.InsertAsync(new Candidate(Guid.NewGuid(), TenantA, "Layla Hassan", "layla@example.com"), autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            (await repo.GetListAsync()).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_attempts_or_results()
    {
        var repo = GetRequiredService<IRepository<Attempt, Guid>>();
        var now = DateTime.UtcNow;

        await AsTenantAsync(TenantA, async () =>
        {
            var attempt = new Attempt(Guid.NewGuid(), TenantA, Guid.NewGuid(), Guid.NewGuid(),
                                      now, now.AddMinutes(60), 12345);
            attempt.ApplyScore(42m, 50m, 60m);
            await repo.InsertAsync(attempt, autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            (await repo.GetListAsync()).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_exam_links()
    {
        // A visible link would be a working way into someone else's exam.
        var repo = GetRequiredService<IRepository<ExamLink, Guid>>();

        await AsTenantAsync(TenantA, async () =>
        {
            await repo.InsertAsync(
                new ExamLink(Guid.NewGuid(), TenantA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                             "hash-a", "prefix-a", DateTime.UtcNow.AddDays(7), 1),
                autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            (await repo.GetListAsync()).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_catalog()
    {
        // A tenant's category and topic names describe its business. Leaking them
        // tells a competitor what the other one assesses for.
        var categories = GetRequiredService<IRepository<Category, Guid>>();
        var topics = GetRequiredService<IRepository<Topic, Guid>>();

        await AsTenantAsync(TenantA, async () =>
        {
            await categories.InsertAsync(new Category(Guid.NewGuid(), TenantA, "spanish", "Spanish"), autoSave: true);
            await topics.InsertAsync(new Topic(Guid.NewGuid(), TenantA, "listening", "Listening"), autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            (await categories.GetListAsync()).ShouldBeEmpty();
            (await topics.GetListAsync()).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task A_tenant_cannot_see_another_tenants_groups_or_answers()
    {
        var groups = GetRequiredService<IRepository<CandidateGroup, Guid>>();
        var answers = GetRequiredService<IRepository<Answer, Guid>>();
        var attempts = GetRequiredService<IRepository<Attempt, Guid>>();

        var attemptId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await AsTenantAsync(TenantA, async () =>
        {
            await groups.InsertAsync(new CandidateGroup(Guid.NewGuid(), TenantA, "Spanish B1 — Autumn 2026"), autoSave: true);

            await attempts.InsertAsync(
                new Attempt(attemptId, TenantA, Guid.NewGuid(), Guid.NewGuid(), now, now.AddMinutes(30), 999),
                autoSave: true);

            await answers.InsertAsync(
                new Answer(Guid.NewGuid(), TenantA, attemptId, Guid.NewGuid()) { Response = "\"a\"" },
                autoSave: true);
        });

        await AsTenantAsync(TenantB, async () =>
        {
            (await groups.GetListAsync()).ShouldBeEmpty();
            (await answers.GetListAsync()).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task Every_assessment_entity_declares_itself_multi_tenant()
    {
        // The failure this whole suite guards against was structural: the entities
        // simply did not implement the interface, so the filter had nothing to act
        // on. Asserting it by reflection means a new entity added later cannot
        // reintroduce the hole by omission — a test fails before it ships.
        var assembly = typeof(Exam).Assembly;

        var assessmentEntities = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Namespace?.StartsWith("InternshipManagementSystem.Assessment") == true)
            .Where(t => typeof(Volo.Abp.Domain.Entities.IEntity).IsAssignableFrom(t))
            .ToList();

        assessmentEntities.ShouldNotBeEmpty();

        var unguarded = assessmentEntities
            .Where(t => !typeof(IMultiTenant).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        unguarded.ShouldBeEmpty(
            $"These assessment entities do not implement IMultiTenant, so ABP's tenant " +
            $"filter will not apply to them and their rows will be visible to every " +
            $"tenant: {string.Join(", ", unguarded)}");
    }

    /// <summary>Runs <paramref name="action"/> as if the request belonged to <paramref name="tenantId"/>.</summary>
    private async Task AsTenantAsync(Guid tenantId, Func<Task> action)
    {
        using (_currentTenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
