using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Getting a bank of questions into the product.
/// <para>
/// The same thing the candidate import does for a roll of students, for the
/// other half of the setup cost: an author's questions are already in a
/// spreadsheet, and retyping eighty of them with four options each is why
/// authoring stops on the first evening.
/// </para>
/// <para>
/// The parser's own rules are covered as unit tests, without a host. What these
/// add is that the service actually applies them — that the payload validator is
/// still in the path, that a dry run really writes nothing, that a second import
/// of the same sheet does not double the bank, and that the example file this
/// server hands out is a file this server can read.
/// </para>
/// </summary>
public class QuestionImportTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000031");

    /// <summary>The headings the generated template writes, in the order it writes them.</summary>
    private const string Header =
        "Type,Question,Option 1,Option 2,Option 3,Option 4,Correct answer,Marks,Difficulty,Explanation";

    public QuestionImportTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_spreadsheet_of_questions_becomes_the_exams_questions()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                Content = Bytes(
                    Header,
                    "single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,1,2,Easy,",
                    "true or false,The Nile is the longest river in Africa.,,,,,true,1,,",
                    "short answer,Name the currency of Egypt.,,,,,\"pound|Egyptian pound\",3,Medium,"),
            });

            result.Created.ShouldBe(3);
            result.Problems.ShouldBeEmpty();

            var listed = await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id });

            listed.TotalCount.ShouldBe(3);

            // Each row produced the type its words meant, not a default.
            listed.Items.Select(q => q.Type).ShouldContain(QuestionTypes.SingleChoice);
            listed.Items.Select(q => q.Type).ShouldContain(QuestionTypes.TrueFalse);
            listed.Items.Select(q => q.Type).ShouldContain(QuestionTypes.FillInTheBlank);

            var single = listed.Items.Single(q => q.Type == QuestionTypes.SingleChoice);

            // The payload is free-form JSON and nothing structural guarantees the
            // key survived the journey, which is exactly why it is worth asserting.
            var spec = PayloadJson.Read<ChoicePayload>(single.Payload);

            spec.ShouldNotBeNull();
            spec!.Options.Count.ShouldBe(4);
            spec.Options.Single(o => o.IsCorrect).Text.ShouldBe("Cairo");

            single.Score.ShouldBe(2m);
            single.Difficulty.ShouldBe(QuestionDifficulty.Easy);
        });
    }

    [Fact]
    public async Task A_dry_run_shows_what_would_be_created_and_writes_nothing()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                DryRun = true,
                Content = Bytes(
                    Header,
                    "single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,1,1,,"),
            });

            result.Created.ShouldBe(1);

            // The whole point of the preview: the author sees the options and the
            // answer this read out of their columns, and can tell at a glance that
            // the key landed on the row they meant. Written out rather than
            // numbered, because a list of numbers looks exactly as right when it
            // is wrong.
            var preview = result.Preview.ShouldHaveSingleItem();

            preview.Line.ShouldBe(2);
            preview.Options.ShouldBe(new[] { "Cairo", "Alexandria", "Aswan", "Tanta" });
            preview.CorrectAnswers.ShouldBe(new[] { "Cairo" });

            (await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id }))
                .TotalCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task A_bad_row_is_reported_with_its_row_number_and_its_column_and_the_rest_still_import()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                Content = Bytes(
                    Header,
                    "single choice,Fine question,A,B,C,D,1,1,,",
                    "single choice,The key names nothing,A,B,C,D,Zebra,1,,",
                    "single choice,Also fine,A,B,C,D,2,1,,"),
            });

            // One bad row must not cost the good ones. That is the difference
            // between an import somebody uses twice and one they abandon.
            result.Created.ShouldBe(2);

            var problem = result.Problems.ShouldHaveSingleItem();

            // The row number is counted over the file, so it is the row the author
            // is looking at in their spreadsheet — the headings being row 1.
            problem.Line.ShouldBe(3);
            problem.Reason.ShouldBe("IMS:QuestionImport:AnswerIsNotOneOfTheOptions");

            // And the column, so the fix is one cell rather than nine.
            problem.Column.ShouldBe(QuestionCsvParser.CorrectColumnKey);
            problem.Content.ShouldContain("The key names nothing");
        });
    }

    [Fact]
    public async Task The_payload_validator_is_still_in_the_path()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            // A row can read cleanly and still describe a question no grader
            // could score. An import must not be a way around the check that
            // exists to stop that reaching a candidate mid-exam.
            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                Content = Bytes(
                    Header,
                    "multiple answers,Everything is right,A,B,C,D,\"1,2,3,4\",1,,"),
            });

            result.Created.ShouldBe(0);
            result.Problems.ShouldHaveSingleItem().Reason.ShouldBe("IMS:Question:AllOptionsCorrect");
        });
    }

    [Fact]
    public async Task Importing_the_same_sheet_twice_does_not_double_the_bank()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var sheet = Bytes(
                Header,
                "single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,1,1,,",
                "single choice,What is the longest river?,Nile,Amazon,Congo,Volga,1,1,,");

            await _questions.ImportAsync(new ImportQuestionsDto { ExamId = exam.Id, Content = sheet });
            var second = await _questions.ImportAsync(new ImportQuestionsDto { ExamId = exam.Id, Content = sheet });

            // Matched by question text and left alone, so an author who corrects
            // six rows and sends the sheet again gets the six rather than
            // eighty-six.
            second.Created.ShouldBe(0);
            second.AlreadyPresent.ShouldBe(2);

            (await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id }))
                .TotalCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_byte_order_mark_from_excel_does_not_hide_the_first_column()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            // Excel's "CSV UTF-8" writes one, and it is the only save format that
            // keeps Arabic readable — so this is not an edge case, it is what an
            // Arabic-speaking author's file looks like every single time.
            var csv = Header + "\r\n" + "اختيار واحد,ما عاصمة مصر؟,القاهرة,أسوان,طنطا,بنها,١,١,سهل,\r\n";

            var withMark = new byte[] { 0xEF, 0xBB, 0xBF }
                .Concat(Encoding.UTF8.GetBytes(csv))
                .ToArray();

            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                Content = withMark,
            });

            result.Problems.ShouldBeEmpty();
            result.Created.ShouldBe(1);

            var created = (await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id }))
                .Items.Single();

            created.Text.ShouldBe("ما عاصمة مصر؟");
            PayloadJson.Read<ChoicePayload>(created.Payload)!
                .Options.Single(o => o.IsCorrect).Text.ShouldBe("القاهرة");
        });
    }

    [Fact]
    public async Task The_example_file_this_hands_out_is_a_file_this_can_read()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            // The drift guard, and the reason the template is generated rather
            // than written by hand. A help page listing headings to type goes
            // stale silently and takes the import with it; this fails instead.
            var template = await _questions.GetImportTemplateAsync();

            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                Content = Encoding.UTF8.GetBytes(template),
            });

            result.Problems.ShouldBeEmpty();

            // One example per type it supports, so an author meets all four.
            result.Created.ShouldBe(4);

            var types = (await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id }))
                .Items.Select(q => q.Type).ToList();

            types.ShouldContain(QuestionTypes.SingleChoice);
            types.ShouldContain(QuestionTypes.MultiSelect);
            types.ShouldContain(QuestionTypes.TrueFalse);
            types.ShouldContain(QuestionTypes.FillInTheBlank);
        });
    }

    [Fact]
    public async Task Questions_can_be_imported_into_the_shared_bank_rather_than_one_exam()
    {
        await AsTenantAsync(async () =>
        {
            var (categoryId, levelId) = await CatalogAsync();

            var result = await _questions.ImportAsync(new ImportQuestionsDto
            {
                CategoryId = categoryId,
                LevelId = levelId,
                Content = Bytes(Header, "single choice,A bank question,A,B,C,D,1,1,,"),
            });

            result.Created.ShouldBe(1);

            var created = (await _questions.GetListAsync(new QuestionListRequestDto { BankOnly = true }))
                .Items.Single();

            // Owned by a domain and a level rather than by a paper, which is what
            // lets every exam at that level draw it.
            created.ExamId.ShouldBeNull();
            created.CategoryId.ShouldBe(categoryId);
            created.LevelId.ShouldBe(levelId);
        });
    }

    [Fact]
    public async Task An_import_that_belongs_nowhere_is_refused_before_anything_is_read()
    {
        await AsTenantAsync(async () =>
        {
            // No exam and no domain: every question would be written and then
            // invisible — no exam draws it and no bank listing shows it.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _questions.ImportAsync(new ImportQuestionsDto
                {
                    Content = Bytes(Header, "single choice,Orphan,A,B,C,D,1,1,,"),
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.QuestionBelongsNowhere);
        });
    }

    [Fact]
    public async Task A_file_with_no_headings_this_recognises_is_refused_outright()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            // Refused as a file rather than as four hundred identical rows, and
            // refused before a single question is written.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _questions.ImportAsync(new ImportQuestionsDto
                {
                    ExamId = exam.Id,
                    Content = Bytes("Name,Email", "Layla,layla@example.com"),
                }));

            thrown.Code.ShouldBe("IMS:QuestionImport:NoQuestionColumn");

            (await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id }))
                .TotalCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task An_imported_question_keeps_its_place_after_the_ones_already_there()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                Type = QuestionTypes.SingleChoice,
                Text = "Written by hand",
                DisplayOrder = 4,
                Payload = PayloadJson.Write(new ChoicePayload
                {
                    Options =
                    [
                        new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                        new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                    ],
                }),
            });

            await _questions.ImportAsync(new ImportQuestionsDto
            {
                ExamId = exam.Id,
                Content = Bytes(Header, "single choice,Imported after it,A,B,C,D,1,1,,"),
            });

            var listed = await _questions.GetListAsync(new QuestionListRequestDto { ExamId = exam.Id });

            // Appended rather than interleaved. An import that dropped everything
            // at position zero would reorder a paper somebody had already
            // arranged, and nothing on the screen would say why.
            listed.Items.Last().Text.ShouldBe("Imported after it");
            listed.Items.Last().DisplayOrder.ShouldBe(5);
        });
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>A sheet, as the bytes a spreadsheet would have written.</summary>
    private static byte[] Bytes(params string[] lines) =>
        Encoding.UTF8.GetBytes(string.Join("\r\n", lines) + "\r\n");

    private async Task<ExamDto> CreateExamAsync() =>
        await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = "Egypt, generally",
            TimeLimitInMinutes = 30,
            PassingPercentage = 60m,
        });

    private async Task<(Guid CategoryId, Guid LevelId)> CatalogAsync()
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();
        var levels = GetRequiredService<IRepository<Level, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, "geography", "geography"),
            autoSave: true);

        var level = await levels.InsertAsync(
            new Level(Guid.NewGuid(), Tenant, "beginner", "beginner") { CategoryId = category.Id },
            autoSave: true);

        return (category.Id, level.Id);
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
