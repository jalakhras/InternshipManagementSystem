using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Catalog.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// The catalogue an organisation files everything under.
/// <para>
/// It had tables and nothing else — no service, no route, no screen — so every
/// exam and every question in the product carried a null category and a null
/// level. That is not cosmetic: <c>Question.DrawableBy</c> collapses to "this
/// exam's own questions" without a category, which left the shared item bank
/// correct, covered by five tests, and unreachable through the product.
/// </para>
/// </summary>
public class CatalogTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICatalogAppService _catalog;
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000021");

    public CatalogTests()
    {
        _catalog = GetRequiredService<ICatalogAppService>();
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_domain_created_here_is_offered_with_its_levels()
    {
        await AsTenantAsync(async () =>
        {
            var english = await _catalog.CreateCategoryAsync(Category("cat-english", "English"));

            await _catalog.CreateLevelAsync(Level("cat-a1", "A1", english.Id, order: 1));
            await _catalog.CreateLevelAsync(Level("cat-a2", "A2", english.Id, order: 2));

            var reloaded = (await _catalog.GetCategoriesAsync()).Single(c => c.Id == english.Id);

            // In ladder order, not alphabetical order. A1 before A2 is the whole
            // point of a level and the reason DisplayOrder exists.
            reloaded.Levels.Select(l => l.Code).ShouldBe(["cat-a1", "cat-a2"]);
        });
    }

    [Fact]
    public async Task A_level_with_no_domain_is_offered_under_every_domain()
    {
        await AsTenantAsync(async () =>
        {
            var first = await _catalog.CreateCategoryAsync(Category("cat-safety", "Safety"));
            var second = await _catalog.CreateCategoryAsync(Category("cat-sales", "Sales"));

            await _catalog.CreateLevelAsync(Level("cat-shared", "Beginner", categoryId: null));

            // An organisation whose ladder is the same across subjects should write
            // it once rather than once per subject.
            var all = await _catalog.GetCategoriesAsync();

            all.Single(c => c.Id == first.Id).Levels.ShouldContain(l => l.Code == "cat-shared");
            all.Single(c => c.Id == second.Id).Levels.ShouldContain(l => l.Code == "cat-shared");
        });
    }

    [Fact]
    public async Task A_domain_reports_what_is_filed_under_it()
    {
        await AsTenantAsync(async () =>
        {
            var category = await _catalog.CreateCategoryAsync(Category("cat-count", "Counted"));
            var level = await _catalog.CreateLevelAsync(Level("cat-count-1", "One", category.Id));

            await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "Filed",
                TimeLimitInMinutes = 30,
                PassingPercentage = 50m,
                CategoryId = category.Id,
                LevelId = level.Id,
            });

            await BankQuestionAsync(category.Id, level.Id);

            var reloaded = (await _catalog.GetCategoriesAsync()).Single(c => c.Id == category.Id);

            // Shown before anybody deactivates it, so "what would this break" is
            // answered on the screen rather than found out afterwards.
            reloaded.ExamCount.ShouldBe(1);
            reloaded.QuestionCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task A_domain_that_exams_are_filed_under_cannot_be_deleted()
    {
        await AsTenantAsync(async () =>
        {
            var category = await _catalog.CreateCategoryAsync(Category("cat-busy", "Busy"));

            await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "Filed here",
                TimeLimitInMinutes = 30,
                PassingPercentage = 50m,
                CategoryId = category.Id,
            });

            // Deleting it would unfile them, and an unfiled exam draws from an empty
            // bank — a paper that silently gets shorter, which is the worst way for
            // this to fail.
            var thrown = await Should.ThrowAsync<BusinessException>(
                async () => await _catalog.DeleteCategoryAsync(category.Id));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.CatalogCategoryInUse);
        });
    }

    [Fact]
    public async Task An_unused_domain_takes_its_levels_with_it()
    {
        await AsTenantAsync(async () =>
        {
            var category = await _catalog.CreateCategoryAsync(Category("cat-gone", "Mistake"));
            await _catalog.CreateLevelAsync(Level("cat-gone-1", "One", category.Id));

            await _catalog.DeleteCategoryAsync(category.Id);

            // The levels described that domain and mean nothing without it.
            (await _catalog.GetCategoriesAsync()).ShouldNotContain(c => c.Id == category.Id);
        });
    }

    [Fact]
    public async Task Two_domains_cannot_share_a_code()
    {
        await AsTenantAsync(async () =>
        {
            await _catalog.CreateCategoryAsync(Category("cat-dup", "First"));

            // The code is what a spreadsheet import matches on, so two of them is a
            // silent misfiling rather than a visible error.
            var thrown = await Should.ThrowAsync<BusinessException>(
                async () => await _catalog.CreateCategoryAsync(Category("cat-dup", "Second")));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.CatalogCodeAlreadyExists);
        });
    }

    [Fact]
    public async Task A_topic_cannot_be_put_inside_its_own_child()
    {
        await AsTenantAsync(async () =>
        {
            var category = await _catalog.CreateCategoryAsync(Category("cat-tree", "Tree"));

            var grammar = await _catalog.CreateTopicAsync(Topic("cat-grammar", "Grammar", category.Id));
            var tenses = await _catalog.CreateTopicAsync(Topic("cat-tenses", "Tenses", category.Id, grammar.Id));

            // Cheap to refuse here. A cycle in the data makes the topic breakdown on
            // a result walk forever, and by then the rows are already written.
            var thrown = await Should.ThrowAsync<BusinessException>(
                async () => await _catalog.UpdateTopicAsync(grammar.Id, new CreateUpdateTopicDto
                {
                    CategoryId = category.Id,
                    Name = "Grammar",
                    Code = "cat-grammar",
                    ParentId = tenses.Id,
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.CatalogTopicCycle);
        });
    }

    [Fact]
    public async Task Deleting_a_topic_promotes_its_children_rather_than_removing_them()
    {
        await AsTenantAsync(async () =>
        {
            var category = await _catalog.CreateCategoryAsync(Category("cat-promote", "Promote"));

            var grammar = await _catalog.CreateTopicAsync(Topic("cat-p-grammar", "Grammar", category.Id));
            var tenses = await _catalog.CreateTopicAsync(Topic("cat-p-tenses", "Tenses", category.Id, grammar.Id));

            await _catalog.DeleteTopicAsync(grammar.Id);

            var topics = (await _catalog.GetCategoriesAsync()).Single(c => c.Id == category.Id).Topics;

            // Removing "grammar" should not take "past perfect" with it. The
            // questions filed under the child are still about something.
            topics.ShouldContain(t => t.Id == tenses.Id);
            topics.Single(t => t.Id == tenses.Id).ParentId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task An_organisation_can_use_its_own_words()
    {
        await AsTenantAsync(async () =>
        {
            var saved = await _catalog.UpdateVocabularyAsync(new UpdateCategorySetDto
            {
                SingularName = "لغة",
                PluralName = "اللغات",
                SubjectSingularName = "متدرّب",
                SubjectPluralName = "المتدرّبون",
                GroupSingularName = "شعبة",
                GroupPluralName = "الشُّعَب",
            });

            saved.GroupSingularName.ShouldBe("شعبة");

            // Read back through the same path every screen uses, because the words
            // are only worth anything if they survive the round trip.
            (await _catalog.GetVocabularyAsync()).SubjectPluralName.ShouldBe("المتدرّبون");
        });
    }

    [Fact]
    public async Task A_tenant_that_never_opened_the_screen_still_has_words()
    {
        await AsTenantAsync(async () =>
        {
            var words = await _catalog.GetVocabularyAsync();

            // Null here would leave every other screen with a blank label.
            words.SingularName.ShouldNotBeNullOrWhiteSpace();
            words.GroupPluralName.ShouldNotBeNullOrWhiteSpace();
        });
    }

    // ------------------------------------------------------------------ helpers

    private static CreateUpdateCategoryDto Category(string code, string name) => new()
    {
        Code = code,
        Name = name,
    };

    private static CreateUpdateLevelDto Level(string code, string name, Guid? categoryId, int order = 0) => new()
    {
        Code = code,
        Name = name,
        CategoryId = categoryId,
        DisplayOrder = order,
    };

    private static CreateUpdateTopicDto Topic(string code, string name, Guid? categoryId, Guid? parentId = null) => new()
    {
        Code = code,
        Name = name,
        CategoryId = categoryId,
        ParentId = parentId,
    };

    private async Task BankQuestionAsync(Guid categoryId, Guid levelId) =>
        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = null,
            CategoryId = categoryId,
            LevelId = levelId,
            Type = QuestionTypes.SingleChoice,
            Text = "Filed under this domain",
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                ],
            }),
        });

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
