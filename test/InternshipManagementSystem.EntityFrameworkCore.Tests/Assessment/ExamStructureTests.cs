using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Sections and named forms, through the real services and a real database.
/// <para>
/// The rules worth testing here are the ones that cannot be undone: a published
/// form must not change, a used form must not be deleted, and deleting a section
/// must not take a term's worth of authoring with it.
/// </para>
/// </summary>
public class ExamStructureTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IExamStructureAppService _structure;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000011");

    public ExamStructureTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _structure = GetRequiredService<IExamStructureAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    // -------------------------------------------------------------- sections

    [Fact]
    public async Task An_exam_can_be_split_into_the_four_skills()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            // Typed in none of the orders the database might hand back by
            // accident: this sequence differs from insertion order, from reverse
            // insertion order, and from alphabetical by name in both directions.
            // It is also what authoring looks like — sections are typed as they
            // are written and sat in an order somebody sets afterwards.
            //
            // Read this before trusting the tick. Deleting
            // `.OrderBy(s => s.DisplayOrder)` from GetSectionsAsync still passes,
            // and no fixture can change that: ExamSection carries a
            // `(ExamId, DisplayOrder)` index, so SQLite answers
            // `WHERE ExamId = @p` by walking that index and returns the rows in
            // DisplayOrder for free. Deleting the index as well makes this fail
            // with ["Grammar", "Listening", "Writing", "Reading"] — the order they
            // were typed in — which is how the coincidence was pinned down. What
            // this test does guard: four sections come back, all of them, named
            // and ordered as authored. What it cannot guard on SQLite is the
            // OrderBy itself, which stays necessary because SQL Server is free to
            // pick a plan that does not supply that order.
            foreach (var (name, order) in new[] { ("Grammar", 2), ("Listening", 0), ("Writing", 3), ("Reading", 1) })
            {
                await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
                {
                    ExamId = exam.Id,
                    Name = name,
                    DisplayOrder = order,
                });
            }

            var sections = await _structure.GetSectionsAsync(exam.Id);

            // Returned in the order they are sat, not in the order they were typed.
            sections.Count.ShouldBe(4);
            sections.Select(s => s.Name).ShouldBe(["Listening", "Reading", "Grammar", "Writing"]);
        });
    }

    [Fact]
    public async Task A_section_can_carry_its_own_clock_and_its_own_floor()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var section = await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
            {
                ExamId = exam.Id,
                Name = "Listening",
                TimeLimitInMinutes = 12,
                MinimumPercentage = 50m,
            });

            // A recording runs for four minutes and the questions on it are
            // answered in eight; a writing task needs twenty and no clock ticking
            // inside it. One exam-wide timer cannot express that.
            section.TimeLimitInMinutes.ShouldBe(12);
            section.MinimumPercentage.ShouldBe(50m);
        });
    }

    [Fact]
    public async Task Deleting_a_section_keeps_its_questions()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var section = await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
            {
                ExamId = exam.Id,
                Name = "Grammar",
            });

            await AddQuestionAsync(exam.Id, "A grammar question", section.Id);

            await _structure.DeleteSectionAsync(section.Id);

            // Deleting a heading must not delete a term's worth of authoring. The
            // questions fall back to the exam and an author decides where they go.
            var remaining = await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id });

            remaining.TotalCount.ShouldBe(1);
            remaining.Items.Single().Text.ShouldBe("A grammar question");
        });
    }

    // ----------------------------------------------------------------- forms

    [Fact]
    public async Task Two_forms_of_one_exam_cannot_share_a_code()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 1", Code = "F1",
            });

            // A code identifies a form on a result sheet, so two sharing one is a
            // result nobody can trace back to a paper.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _structure.CreateFormAsync(new CreateUpdateExamFormDto
                {
                    ExamId = exam.Id, Name = "Form 2", Code = "F1",
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamFormCodeTaken);
        });
    }

    [Fact]
    public async Task A_form_can_be_generated_from_the_bank_then_published()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            await AddQuestionAsync(exam.Id, "First");
            await AddQuestionAsync(exam.Id, "Second");

            var form = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 1", Code = "F1",
            });

            var generated = await _structure.GenerateFormAsync(form.Id, new GenerateExamFormDto { Seed = 42 });

            generated.Questions.Count.ShouldBe(2);
            generated.WasGenerated.ShouldBeTrue();

            var published = await _structure.PublishFormAsync(form.Id);

            published.Status.ShouldBe(ExamFormStatus.Published);

            // Frozen at publish, because a question's marks can be edited next
            // month and a result must keep meaning what it meant on the day.
            published.MaxScore.ShouldBe(generated.Questions.Sum(q => q.Score));
        });
    }

    [Fact]
    public async Task The_same_seed_produces_the_same_paper()
    {
        await AsTenantAsync(async () =>
        {
            // Twenty in the bank, six on a form. The draw has to actually choose
            // for a seed to mean anything: with six questions and no stated size
            // every form was the whole bank in the same order, so this passed
            // with the seed set to zero — it proved the paper was stable, not
            // that the seed was what stabilised it.
            var exam = await _exams.CreateAsync(new CreateUpdateExamDto
            {
                Title = "English A2",
                TimeLimitInMinutes = 45,
                PassingPercentage = 60m,
                QuestionsPerForm = 6,
            });

            for (var i = 0; i < 20; i++)
            {
                await AddQuestionAsync(exam.Id, $"Question {i}");
            }

            var first = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 1", Code = "F1",
            });

            var second = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 2", Code = "F2",
            });

            var a = await _structure.GenerateFormAsync(first.Id, new GenerateExamFormDto { Seed = 7 });
            var b = await _structure.GenerateFormAsync(second.Id, new GenerateExamFormDto { Seed = 7 });

            // What lets a form be regenerated after an edit without becoming an
            // entirely different exam.
            a.Questions.Select(q => q.QuestionId).ShouldBe(b.Questions.Select(q => q.QuestionId));

            var third = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 3", Code = "F3",
            });

            var c = await _structure.GenerateFormAsync(third.Id, new GenerateExamFormDto { Seed = 8 });

            // And the half without which the first is empty: a different seed
            // draws a different paper. A product that ignored the seed entirely
            // would satisfy "the same seed gives the same paper" perfectly.
            c.Questions.Select(q => q.QuestionId)
                .ShouldNotBe(a.Questions.Select(q => q.QuestionId));
        });
    }

    [Fact]
    public async Task A_published_form_cannot_be_edited()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            await AddQuestionAsync(exam.Id, "Only question");

            var form = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 1", Code = "F1",
            });

            await _structure.GenerateFormAsync(form.Id, new GenerateExamFormDto());
            await _structure.PublishFormAsync(form.Id);

            // Two candidates who sat "Form 1" must have answered the same paper.
            // Without this the only thing a form was for is gone.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _structure.GenerateFormAsync(form.Id, new GenerateExamFormDto()));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamFormNotEditable);
        });
    }

    [Fact]
    public async Task An_empty_form_cannot_be_published()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var form = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Empty", Code = "F0",
            });

            var thrown = await Should.ThrowAsync<BusinessException>(() => _structure.PublishFormAsync(form.Id));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamFormHasNoQuestions);
        });
    }

    [Fact]
    public async Task A_question_the_exam_cannot_draw_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var mine = await CreateExamAsync("Mine");
            var theirs = await CreateExamAsync("Theirs");

            var stranger = await AddQuestionAsync(theirs.Id, "Belongs elsewhere");

            var form = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = mine.Id, Name = "Form 1", Code = "F1",
            });

            // Putting another exam's question on this paper is the cross-boundary
            // leak in miniature.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _structure.SetFormQuestionsAsync(form.Id, new SetExamFormQuestionsDto
                {
                    QuestionIds = [stranger],
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamFormQuestionNotAvailable);
        });
    }

    [Fact]
    public async Task Chosen_questions_keep_the_order_they_were_given_in()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var first = await AddQuestionAsync(exam.Id, "Asked first");
            var second = await AddQuestionAsync(exam.Id, "Asked second");

            var form = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Form 1", Code = "F1",
            });

            // Reversed on purpose: the caller's order is the paper's order.
            var detail = await _structure.SetFormQuestionsAsync(form.Id, new SetExamFormQuestionsDto
            {
                QuestionIds = [second, first],
            });

            detail.Questions.Select(q => q.QuestionId).ShouldBe([second, first]);
            detail.WasGenerated.ShouldBeFalse();
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<ExamDto> CreateExamAsync(string title = "English A2") =>
        await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = title,
            TimeLimitInMinutes = 45,
            PassingPercentage = 60m,
        });

    private async Task<Guid> AddQuestionAsync(Guid examId, string text, Guid? sectionId = null)
    {
        var created = await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = examId,
            Type = QuestionTypes.SingleChoice,
            Text = text,
            Score = 1m,
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                ],
            }),
        });

        if (sectionId is { } id)
        {
            await _questions.UpdateAsync(created.Id, new CreateUpdateQuestionDto
            {
                ExamId = examId,
                ExamSectionId = id,
                Type = created.Type,
                Text = created.Text,
                Score = created.Score,
                Payload = created.Payload,
            });
        }

        return created.Id;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
