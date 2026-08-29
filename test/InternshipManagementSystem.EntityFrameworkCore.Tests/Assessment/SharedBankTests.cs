using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Whether a question written into the bank actually reaches a paper.
/// <para>
/// Written after a business review read the code and found it did not. The bank
/// existed in the schema, the domain carried a rule about what an exam may draw,
/// and nothing called it — every path filtered on the owning exam alone. So
/// "three forms for one level draw from one bank" was true of the database and
/// false of the product, and no test said otherwise because no test crossed from
/// authoring into delivery.
/// </para>
/// <para>
/// These do. They are integration tests on purpose: the rule has to survive
/// being translated into SQL, which is exactly where a unit test would not have
/// caught it.
/// </para>
/// </summary>
public class SharedBankTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000007");

    public SharedBankTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_exam_counts_the_bank_questions_its_domain_and_level_offer()
    {
        await AsTenantAsync(async () =>
        {
            var (categoryId, levelId) = await CatalogAsync("english", "a1");
            var exam = await CreateExamAsync(categoryId, levelId);

            await BankQuestionAsync(categoryId, levelId, "Level A1 grammar");
            await BankQuestionAsync(categoryId, null, "Suits any level in this domain");

            // Another domain entirely. It must not appear, or a language centre
            // would find trading questions on its placement test.
            var (otherCategoryId, _) = await CatalogAsync("trading", "beginner");
            await BankQuestionAsync(otherCategoryId, null, "Chart reading");

            var reloaded = await _exams.GetAsync(exam.Id);

            reloaded.QuestionCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_bank_question_for_another_level_is_not_offered()
    {
        await AsTenantAsync(async () =>
        {
            var (categoryId, a1) = await CatalogAsync("spanish", "s-a1");
            var b1 = await LevelAsync(categoryId, "s-b1");

            var exam = await CreateExamAsync(categoryId, a1);

            await BankQuestionAsync(categoryId, a1, "For A1");
            await BankQuestionAsync(categoryId, b1, "For B1");

            // A question written for B1 is not easier or harder by accident; it is
            // about material an A1 student has not met.
            (await _exams.GetAsync(exam.Id)).QuestionCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Two_exams_at_one_level_draw_from_the_same_bank()
    {
        await AsTenantAsync(async () =>
        {
            var (categoryId, levelId) = await CatalogAsync("french", "f-a2");

            var formOne = await CreateExamAsync(categoryId, levelId, "Form 1");
            var formTwo = await CreateExamAsync(categoryId, levelId, "Form 2");

            await BankQuestionAsync(categoryId, levelId, "Shared one");
            await BankQuestionAsync(categoryId, levelId, "Shared two");

            // The whole point of the bank: two forms over one pool rather than two
            // copies that drift apart the first time a key is corrected.
            (await _exams.GetAsync(formOne.Id)).QuestionCount.ShouldBe(2);
            (await _exams.GetAsync(formTwo.Id)).QuestionCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_bank_question_lets_an_exam_of_its_own_publish()
    {
        await AsTenantAsync(async () =>
        {
            var (categoryId, levelId) = await CatalogAsync("safety", "s-1");
            var exam = await CreateExamAsync(categoryId, levelId);

            await BankQuestionAsync(categoryId, levelId, "Where is the assembly point?");

            // The publish check used to count only the exam's own questions, so an
            // exam whose entire paper came from the bank was refused for having
            // none — the reason naming a problem the author could not see.
            var check = await _exams.CheckPublishAsync(exam.Id);

            check.Blockers.ShouldNotContain(InternshipManagementSystemDomainErrorCodes.ExamHasNoQuestions);
            check.CanPublish.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task A_question_owned_by_one_exam_stays_with_it()
    {
        await AsTenantAsync(async () =>
        {
            var (categoryId, levelId) = await CatalogAsync("history", "h-1");

            var mine = await CreateExamAsync(categoryId, levelId, "Mine");
            var theirs = await CreateExamAsync(categoryId, levelId, "Theirs");

            await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = mine.Id,
                Type = QuestionTypes.Text,
                Text = "Written straight into one exam",
            });

            // Owning an exam is still meaningful. A question written into one paper
            // does not leak into every other paper at the same level.
            (await _exams.GetAsync(mine.Id)).QuestionCount.ShouldBe(1);
            (await _exams.GetAsync(theirs.Id)).QuestionCount.ShouldBe(0);
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<(Guid CategoryId, Guid LevelId)> CatalogAsync(string categoryCode, string levelCode)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, categoryCode, categoryCode),
            autoSave: true);

        return (category.Id, await LevelAsync(category.Id, levelCode));
    }

    private async Task<Guid> LevelAsync(Guid categoryId, string code)
    {
        var levels = GetRequiredService<IRepository<Level, Guid>>();

        var level = await levels.InsertAsync(
            new Level(Guid.NewGuid(), Tenant, code, code) { CategoryId = categoryId },
            autoSave: true);

        return level.Id;
    }

    private async Task<ExamDto> CreateExamAsync(Guid categoryId, Guid levelId, string title = "Placement") =>
        await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = title,
            TimeLimitInMinutes = 30,
            PassingPercentage = 60m,
            CategoryId = categoryId,
            LevelId = levelId,
        });

    private async Task BankQuestionAsync(Guid categoryId, Guid? levelId, string text) =>
        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            // No exam. This is what makes it a bank question.
            ExamId = null,
            CategoryId = categoryId,
            LevelId = levelId,
            Type = QuestionTypes.SingleChoice,
            Text = text,
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
