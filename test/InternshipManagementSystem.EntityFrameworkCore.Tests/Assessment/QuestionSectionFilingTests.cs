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
    /// A section of one exam cannot hold a question of another.
    /// <para>
    /// No screen can produce this: the picker in the question form is fed from
    /// <c>getSections(examId)</c> and offers this exam's parts only. The API could,
    /// and did — it read <c>ExamSectionId</c> off the body and assigned it, so any
    /// caller holding <c>Questions.Create</c> could file a question into a
    /// neighbouring exam's part by pasting one id, and in an installation serving
    /// several organisations, into another organisation's.
    /// </para>
    /// <para>
    /// The damage is silent in both directions. <c>DrawBySection</c> pools on
    /// <c>q.ExamSectionId == section.Id</c> over the owning exam's bank, so the
    /// question is drawn by neither exam and simply leaves both papers, while the
    /// other exam's structure screen counts it and reports a part that can fill
    /// itself when it cannot.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_question_cannot_be_filed_into_a_section_of_a_different_exam()
    {
        await AsTenantAsync(async () =>
        {
            var mine = await CreateExamAsync();
            var theirs = await CreateExamAsync();

            var theirListening = await CreateSectionAsync(theirs.Id, "Their listening");

            var refused = await Should.ThrowAsync<BusinessException>(async () =>
                await _questions.CreateAsync(
                    FiledQuestion(mine.Id, theirListening.Id, "Filed across the fence")));

            refused.Code.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.QuestionSectionNotInExam);

            // And nothing was written on the way to being refused. The other exam's
            // part still reports the count its own author would recognise.
            var theirSections = await _structure.GetSectionsAsync(theirs.Id);
            theirSections.Single().QuestionCount.ShouldBe(0);
        });
    }

    /// <summary>
    /// The same on the way through an edit, which is the easier half to forget.
    /// <para>
    /// Checked against the exam the question already belongs to rather than the one
    /// the body names, because <c>Apply</c> never moves a question between exams:
    /// the body's <c>ExamId</c> is a claim the update path does not act on, so
    /// trusting it here would let a caller name the section's exam in the body and
    /// pass a check that decided nothing.
    /// </para>
    /// </summary>
    [Fact]
    public async Task An_edit_cannot_move_a_question_into_another_exams_section()
    {
        await AsTenantAsync(async () =>
        {
            var mine = await CreateExamAsync();
            var theirs = await CreateExamAsync();

            var myReading = await CreateSectionAsync(mine.Id, "My reading");
            var theirReading = await CreateSectionAsync(theirs.Id, "Their reading");

            var created = await _questions.CreateAsync(
                FiledQuestion(mine.Id, myReading.Id, "Properly filed to begin with"));

            var edit = AsTheFormResends(await _questions.GetAsync(created.Id));
            edit.ExamSectionId = theirReading.Id;

            // Naming the other exam in the body as well, which is the shape that
            // would slip past a check reading input.ExamId.
            edit.ExamId = theirs.Id;

            var refused = await Should.ThrowAsync<BusinessException>(async () =>
                await _questions.UpdateAsync(created.Id, edit));

            refused.Code.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.QuestionSectionNotInExam);

            // Refused rather than partly applied: the question is still where its
            // author put it.
            (await _questions.GetAsync(created.Id)).ExamSectionId.ShouldBe(myReading.Id);
        });
    }

    /// <summary>
    /// A section id that names nothing is refused by the same code.
    /// <para>
    /// Deliberately not a separate "no such section". Under the tenant filter another
    /// organisation's section is simply not found, and two different answers would
    /// tell a caller which of the two it was — which is an answer about somebody
    /// else's data, given to someone who asked by guessing.
    /// </para>
    /// </summary>
    [Fact]
    public async Task A_section_that_does_not_exist_is_refused_the_same_way()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var refused = await Should.ThrowAsync<BusinessException>(async () =>
                await _questions.CreateAsync(
                    FiledQuestion(exam.Id, Guid.NewGuid(), "Filed into thin air")));

            refused.Code.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.QuestionSectionNotInExam);
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
