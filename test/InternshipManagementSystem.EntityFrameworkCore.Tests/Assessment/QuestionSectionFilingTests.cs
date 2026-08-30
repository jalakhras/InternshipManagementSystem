using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Filing a question into a section, through the authoring API an author uses.
/// <para>
/// The section tests that already exist put <c>ExamSectionId</c> straight into
/// their fixtures, which is why twenty-two of them were green while no author
/// could file a single question: they never touch the authoring path, and the
/// authoring path was where the field went missing. Everything here goes through
/// <see cref="IQuestionAppService"/> instead, with the body shaped exactly as the
/// question form sends it.
/// </para>
/// </summary>
public class QuestionSectionFilingTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IExamStructureAppService _structure;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000031");

    public QuestionSectionFilingTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _structure = GetRequiredService<IExamStructureAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    /// <summary>
    /// The defect this class is named for: correcting a typo unfiled the question.
    /// <para>
    /// <c>Apply</c> assigns the section unconditionally from the body it is given,
    /// so a client that reads a question and posts it back without the section does
    /// not leave the section alone — it clears it. Nothing about that is visible at
    /// the moment it happens. The author sees a saved question, and the part of the
    /// paper it belonged to quietly has one fewer question to draw on.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Fixing_a_typo_does_not_unfile_the_question_from_its_section()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            var listening = await CreateSectionAsync(exam.Id, "Listening");

            var created = await _questions.CreateAsync(FiledQuestion(exam.Id, listening.Id, "Whcih is the capital?"));

            // The form reads the question back before it edits it, so the section
            // has to survive the projection too. It is the only way a picker can
            // open showing what the question is already filed under.
            var asOpened = await _questions.GetAsync(created.Id);
            asOpened.ExamSectionId.ShouldBe(listening.Id);

            var edit = AsTheFormResends(asOpened);
            edit.Text = "Which is the capital?";

            await _questions.UpdateAsync(created.Id, edit);

            var afterSave = await _questions.GetAsync(created.Id);
            afterSave.Text.ShouldContain("Which is the capital?");
            afterSave.ExamSectionId.ShouldBe(listening.Id);

            // And the section still counts it. This is the number the structure
            // screen shows as "N available", and the one that read zero for every
            // section of every exam for as long as nothing could write the field.
            var sections = await _structure.GetSectionsAsync(exam.Id);
            sections.Single(s => s.Id == listening.Id).QuestionCount.ShouldBe(1);
        });
    }

    /// <summary>
    /// The other direction, which the same unconditional assignment makes work:
    /// clearing the picker really unfiles the question rather than being ignored.
    /// Without this, "unfiled" would be a choice an author could not go back to.
    /// </summary>
    [Fact]
    public async Task Clearing_the_picker_returns_the_question_to_unfiled()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            var reading = await CreateSectionAsync(exam.Id, "Reading");

            var created = await _questions.CreateAsync(FiledQuestion(exam.Id, reading.Id, "Filed to begin with"));

            var edit = AsTheFormResends(await _questions.GetAsync(created.Id));
            edit.ExamSectionId = null;

            await _questions.UpdateAsync(created.Id, edit);

            (await _questions.GetAsync(created.Id)).ExamSectionId.ShouldBeNull();
            (await _structure.GetSectionsAsync(exam.Id)).Single().QuestionCount.ShouldBe(0);
        });
    }

    /// <summary>
    /// What the question list's section filter asks for.
    /// <para>
    /// The filter has been on the server all along and had no caller. Worth pinning
    /// because the listing also serves the shared bank, and a section filter that
    /// let unfiled bank questions through would tell an author a part of the paper
    /// is full when it is empty.
    /// </para>
    /// </summary>
    [Fact]
    public async Task The_list_can_be_narrowed_to_one_part_of_the_paper()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            var listening = await CreateSectionAsync(exam.Id, "Listening");
            var grammar = await CreateSectionAsync(exam.Id, "Grammar");

            await _questions.CreateAsync(FiledQuestion(exam.Id, listening.Id, "Heard on the recording"));
            await _questions.CreateAsync(FiledQuestion(exam.Id, grammar.Id, "Choose the right tense"));
            await _questions.CreateAsync(FiledQuestion(exam.Id, null, "Never filed anywhere"));

            var filed = await _questions.GetListAsync(new QuestionListRequestDto
            {
                ExamId = exam.Id,
                ExamSectionId = listening.Id,
            });

            filed.Items.Select(q => q.Text).ShouldHaveSingleItem().ShouldContain("Heard on the recording");

            // Unfiltered still shows all three, so the filter narrows rather than
            // the section quietly owning the whole exam.
            var everything = await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id });
            everything.TotalCount.ShouldBe(3);
        });
    }

    /// <summary>
    /// The update body the question form sends, rebuilt from what it just read.
    /// <para>
    /// Copied field by field on purpose, because that is exactly what the form
    /// does — it fills its own model from the fetched question and posts the model
    /// back. So a field missing from this list is not a field left alone by the
    /// server; it is a field erased on the next save. Keeping the shapes identical
    /// is the point: this method is the client, standing in.
    /// </para>
    /// </summary>
    private static CreateUpdateQuestionDto AsTheFormResends(QuestionDto question) => new()
    {
        ExamId = question.ExamId,
        CategoryId = question.CategoryId,
        LevelId = question.LevelId,
        ExamSectionId = question.ExamSectionId,
        QuestionGroupId = question.QuestionGroupId,
        Text = question.Text,
        Type = question.Type,
        Payload = question.Payload,
        TopicId = question.TopicId,
        Difficulty = question.Difficulty,
        Score = question.Score,
        Explanation = question.Explanation,
        TimeLimitInSeconds = question.TimeLimitInSeconds,
        MediaBlobName = question.MediaBlobName,
        MediaType = question.MediaType,
        DisplayOrder = question.DisplayOrder,
        IsActive = question.IsActive,
    };

    private static CreateUpdateQuestionDto FiledQuestion(Guid examId, Guid? sectionId, string text) => new()
    {
        ExamId = examId,
        ExamSectionId = sectionId,
        Type = QuestionTypes.SingleChoice,
        Text = text,
        Score = 1m,
        Payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "Cairo", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "Alexandria", IsCorrect = false },
            ],
        }),
    };

    private async Task<ExamSectionDto> CreateSectionAsync(Guid examId, string name) =>
        await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
        {
            ExamId = examId,
            Name = name,
        });

    private async Task<ExamDto> CreateExamAsync() =>
        await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = "Spanish B1 Placement",
            TimeLimitInMinutes = 45,
            PassingPercentage = 60m,
        });

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
