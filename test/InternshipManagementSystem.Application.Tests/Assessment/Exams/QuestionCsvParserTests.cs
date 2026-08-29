using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.Grading.Graders;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// Reading an author's spreadsheet.
/// <para>
/// Every case here is a thing somebody's file actually contains: a byte-order
/// mark Excel put there, a semicolon because their Windows writes one, an answer
/// keyed by the option's number rather than its text, and Arabic spelled the way
/// their keyboard spells it rather than the way ours does.
/// </para>
/// <para>
/// The rule the whole class is built around is that <b>no cell may require
/// programming skill</b>. So the tests are written from the outside: what a
/// person types, and whether it produced the question they meant.
/// </para>
/// </summary>
public class QuestionCsvParserTests
{
    private readonly QuestionCsvParser _parser = new();

    /// <summary>The English headings, in the order the generated template writes them.</summary>
    private const string EnglishHeader =
        "Type,Question,Option 1,Option 2,Option 3,Option 4,Correct answer,Marks,Difficulty,Explanation";

    // ------------------------------------------------------------ the basics

    [Fact]
    public void A_single_choice_row_becomes_a_question_with_the_named_option_marked()
    {
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,1,1,Easy,");

        var draft = Only(sheet);

        draft.Type.ShouldBe(QuestionTypes.SingleChoice);
        draft.Text.ShouldBe("What is the capital of Egypt?");
        draft.Options.Count.ShouldBe(4);
        draft.Options.Single(o => o.IsCorrect).Text.ShouldBe("Cairo");
    }

    [Fact]
    public void The_correct_answer_may_be_written_out_rather_than_numbered()
    {
        // The form an author is *least* likely to key to the wrong row, and the
        // one they reach for first. Refusing it would make the sheet a format to
        // learn rather than a table to fill in.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,Aswan,1,,");

        Only(sheet).Options.Single(o => o.IsCorrect).Text.ShouldBe("Aswan");
    }

    [Fact]
    public void The_correct_answer_may_be_the_options_letter()
    {
        // A sheet exported from another product very often labels the options
        // A, B, C and keys the answer by letter.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,Pick one,Alpha,Beta,Gamma,Delta,C,1,,");

        Only(sheet).Options.Single(o => o.IsCorrect).Text.ShouldBe("Gamma");
    }

    [Fact]
    public void Several_answers_are_marked_on_a_multiple_answer_question()
    {
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "multiple answers,Which cities are in Egypt?,Cairo,Beirut,Aswan,Amman,\"1,3\",2,,");

        var draft = Only(sheet);

        draft.Type.ShouldBe(QuestionTypes.MultiSelect);
        draft.Options.Where(o => o.IsCorrect).Select(o => o.Text)
            .ShouldBe(new[] { "Cairo", "Aswan" });
    }

    [Fact]
    public void True_or_false_needs_no_option_columns_at_all()
    {
        // Nobody should type "True" and "False" eighty times down two columns.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "true or false,The Nile is the longest river in Africa.,,,,,true,1,,");

        var draft = Only(sheet);

        draft.Type.ShouldBe(QuestionTypes.TrueFalse);
        draft.Options.Select(o => o.Text).ShouldBe(new[] { "True", "False" });
        draft.Options.Single(o => o.IsCorrect).Text.ShouldBe("True");
    }

    [Fact]
    public void A_short_answer_with_accepted_spellings_is_marked_by_machine()
    {
        // With answers listed the question can be scored automatically, which is
        // what an author importing a bank of eighty wants. The type follows what
        // they wrote rather than a setting they would have to find.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "short answer,Name the currency of Egypt.,,,,,\"pound|Egyptian pound\",2,,");

        var draft = Only(sheet);

        draft.Type.ShouldBe(QuestionTypes.FillInTheBlank);
        draft.AcceptedAnswers.ShouldBe(new[] { "pound", "Egyptian pound" });
    }

    [Fact]
    public void A_short_answer_with_no_key_is_left_for_a_person_to_mark()
    {
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "short answer,Explain why the Nile floods.,,,,,,5,,");

        var draft = Only(sheet);

        // Both are legitimate questions and the author meant one of them. An
        // empty answer column means nobody wrote a key, which is what a written
        // answer is.
        draft.Type.ShouldBe(QuestionTypes.Text);
        draft.AcceptedAnswers.ShouldBeEmpty();
    }

    // ------------------------------------------- what the spreadsheet does to it

    [Fact]
    public void A_byte_order_mark_does_not_hide_the_first_column()
    {
        // Excel's "CSV UTF-8" writes one. Left in place it lands on the Type
        // heading, the column is not found, and the author is told their file has
        // no columns while looking at a file that plainly does.
        var csv = EnglishHeader + "\n" + "single choice,Two plus two?,3,4,5,6,2,1,,";
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

        Only(_parser.Read(bytes)).Options.Single(o => o.IsCorrect).Text.ShouldBe("4");
    }

    [Fact]
    public void Semicolons_separate_the_columns_where_that_is_what_excel_wrote()
    {
        // Excel writes a semicolon wherever the system decimal separator is a
        // comma. Asking the author which one they have is asking them to know
        // something nothing on their screen tells them.
        var sheet = _parser.Read(
            EnglishHeader.Replace(',', ';') + "\n" +
            "single choice;Two plus two?;3;4;5;6;2;1;;");

        Only(sheet).Options.Single(o => o.IsCorrect).Text.ShouldBe("4");
    }

    [Fact]
    public void Tabs_separate_the_columns_where_the_file_was_pasted_out_of_a_sheet()
    {
        var sheet = _parser.Read(
            EnglishHeader.Replace(',', '\t') + "\n" +
            "single choice\tTwo plus two?\t3\t4\t5\t6\t2\t1\t\t");

        Only(sheet).Options.Single(o => o.IsCorrect).Text.ShouldBe("4");
    }

    [Fact]
    public void A_question_containing_a_comma_survives_intact()
    {
        // A prompt with a comma in it is the normal case, not the exception. A
        // reader that lost half of every question would be worse than no import.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,\"After the flood, what did they plant?\",Wheat,Rice,Cotton,Barley,1,1,,");

        Only(sheet).Text.ShouldBe("After the flood, what did they plant?");
    }

    [Fact]
    public void A_quoted_question_may_run_over_two_lines_without_shifting_the_row_numbers()
    {
        // The row number is where the record *started*. Counting the wrapped line
        // would put every reported problem below it one row out, and an author
        // would go and correct a question that was fine.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,\"A prompt\nover two lines\",A,B,C,D,1,1,,\n" +
            "single choice,Broken,A,B,C,D,9,1,,");

        sheet.Rows[0].Line.ShouldBe(2);
        sheet.Rows[1].Line.ShouldBe(4);
    }

    [Fact]
    public void Blank_rows_under_the_last_question_are_not_reported_as_problems()
    {
        // What a spreadsheet leaves behind. A red line under a file that is
        // perfectly fine teaches people to ignore the red lines.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,Two plus two?,3,4,5,6,2,1,,\n" +
            ",,,,,,,,,\n" +
            ",,,,,,,,,\n");

        sheet.Rows.Count.ShouldBe(1);
    }

    // ------------------------------------------------------------ the language

    [Fact]
    public void Arabic_headings_are_read_however_the_authors_keyboard_spells_them()
    {
        // Optional vowel marks, three spellings of alef, two of the final ha and
        // two sets of digits — none of which change the word. Matching the raw
        // text would mean two authors could not use the same sheet and neither
        // would ever find out why.
        var sheet = _parser.Read(
            "النوع,السؤال,خيار ١,خيار ٢,خيار ٣,خيار ٤,الاجابه الصحيحه,الدرجة,الصعوبة,التفسير\n" +
            "اختيار واحد,ما عاصمة مصر؟,القاهرة,الإسكندرية,أسوان,طنطا,١,٢٫٥,سهل,");

        var draft = Only(sheet);

        draft.Options.Single(o => o.IsCorrect).Text.ShouldBe("القاهرة");
        draft.Score.ShouldBe(2.5m);
        draft.Difficulty.ShouldBe(QuestionDifficulty.Easy);
    }

    [Fact]
    public void An_arabic_true_or_false_gets_arabic_options()
    {
        // A bank of Arabic questions whose options read "True" and "False" looks
        // like somebody else's exam pasted into theirs.
        var sheet = _parser.Read(
            "النوع,السؤال,الإجابة الصحيحة\n" +
            "صح أو خطأ,النيل أطول أنهار أفريقيا.,صح");

        Only(sheet).Options.Select(o => o.Text).ShouldBe(new[] { "صح", "خطأ" });
    }

    [Fact]
    public void An_arabic_answer_may_list_its_options_with_an_arabic_comma()
    {
        // What an Arabic keyboard produces. A rule written only for the Latin
        // punctuation would silently throw the second answer away.
        var sheet = _parser.Read(
            "النوع,السؤال,خيار 1,خيار 2,خيار 3,خيار 4,الإجابة الصحيحة\n" +
            "اختيار متعدد,أي من هذه المدن تقع في مصر؟,القاهرة,بيروت,أسوان,عمّان,\"١،٣\"");

        Only(sheet).Options.Where(o => o.IsCorrect).Select(o => o.Text)
            .ShouldBe(new[] { "القاهرة", "أسوان" });
    }

    [Fact]
    public void The_headings_the_template_writes_are_headings_this_can_read()
    {
        // The drift guard. The template is generated from the localisation files
        // and the parser matches its own list of words, so nothing structural
        // stops the two parting company — and the way that fails is that the
        // example file we hand somebody is a file we refuse.
        foreach (var language in new[] { "en", "ar" })
        {
            var texts = TextsFor(language);

            var header = string.Join(',',
                texts["QuestionImport:Column:Type"],
                texts["QuestionImport:Column:Question"],
                texts["QuestionImport:Column:Option"].Replace("{0}", "1"),
                texts["QuestionImport:Column:Option"].Replace("{0}", "2"),
                texts["QuestionImport:Column:Option"].Replace("{0}", "3"),
                texts["QuestionImport:Column:Option"].Replace("{0}", "4"),
                texts["QuestionImport:Column:Correct"],
                texts["QuestionImport:Column:Marks"],
                texts["QuestionImport:Column:Difficulty"],
                texts["QuestionImport:Column:Explanation"]);

            var options = texts["QuestionImport:Sample:1:Options"].Split('|');

            var row = string.Join(',',
                texts["QuestionImport:Sample:1:Type"],
                Quote(texts["QuestionImport:Sample:1:Question"]),
                options[0], options[1], options[2], options[3],
                texts["QuestionImport:Sample:1:Correct"],
                texts["QuestionImport:Sample:1:Marks"],
                texts["QuestionImport:Sample:1:Difficulty"],
                Quote(texts["QuestionImport:Sample:1:Explanation"]));

            var draft = Only(_parser.Read(header + "\n" + row));

            draft.Type.ShouldBe(QuestionTypes.SingleChoice, $"the {language} template is not readable");
            draft.Options.Count.ShouldBe(4);
            draft.Options[0].IsCorrect.ShouldBeTrue();
        }
    }

    // --------------------------------------------------- what goes wrong, per row

    [Fact]
    public void One_bad_row_does_not_cost_the_good_ones()
    {
        // The difference between an import somebody uses twice and one they
        // abandon. Nothing about a wrong row on line 3 makes line 2 unreadable.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,Fine question,A,B,C,D,1,1,,\n" +
            "single choice,Answer names nothing,A,B,C,D,Zebra,1,,\n" +
            "single choice,Also fine,A,B,C,D,2,1,,");

        sheet.Rows.Count(row => row.Question is not null).ShouldBe(2);

        var problem = sheet.Rows.Single(row => row.Reason is not null);

        // The row number is counted over the file, so it is the one the author is
        // looking at in their spreadsheet — the headings being row 1.
        problem.Line.ShouldBe(3);
        problem.Reason.ShouldBe("IMS:QuestionImport:AnswerIsNotOneOfTheOptions");

        // And the column, so the fix is one cell rather than nine.
        problem.Column.ShouldBe(QuestionCsvParser.CorrectColumnKey);
    }

    [Fact]
    public void A_single_choice_row_naming_two_answers_is_refused_with_the_key_the_form_uses()
    {
        // The grader wants exactly one selection, so a key naming two fails every
        // candidate. The same code a hand-written question would raise, so one
        // mistake reads as one sentence however it arrived.
        var sheet = _parser.Read(
            EnglishHeader + "\n" +
            "single choice,Pick one,A,B,C,D,\"1,2\",1,,");

        var problem = sheet.Rows.Single();

        problem.Reason.ShouldBe("IMS:Question:SingleChoiceHasManyCorrect");
        problem.Column.ShouldBe(QuestionCsvParser.CorrectColumnKey);
    }

    [Fact]
    public void Marking_every_option_correct_is_refused()
    {
        // Ticking everything would be right, which measures nothing.
        _parser.Read(EnglishHeader + "\n" + "multiple answers,All of them,A,B,C,D,\"1,2,3,4\",1,,")
            .Rows.Single().Reason.ShouldBe("IMS:Question:AllOptionsCorrect");
    }

    [Fact]
    public void A_choice_row_with_one_option_is_refused()
    {
        _parser.Read(EnglishHeader + "\n" + "single choice,Only one,A,,,,1,1,,")
            .Rows.Single().Reason.ShouldBe("IMS:Question:NeedsTwoOptions");
    }

    [Fact]
    public void A_choice_row_with_no_answer_is_refused()
    {
        // Would score every candidate zero and look like a hard question rather
        // than a broken one.
        _parser.Read(EnglishHeader + "\n" + "single choice,No key,A,B,C,D,,1,,")
            .Rows.Single().Reason.ShouldBe("IMS:Question:NoCorrectOption");
    }

    [Fact]
    public void Two_identical_options_on_one_row_are_refused()
    {
        _parser.Read(EnglishHeader + "\n" + "single choice,Repeated,Cairo,Cairo,Aswan,Tanta,1,1,,")
            .Rows.Single().Reason.ShouldBe("IMS:QuestionImport:RepeatedOption");
    }

    [Fact]
    public void A_row_with_no_question_written_in_it_is_refused_against_the_question_column()
    {
        var problem = _parser.Read(EnglishHeader + "\n" + "single choice,,A,B,C,D,1,1,,").Rows.Single();

        problem.Reason.ShouldBe("IMS:QuestionImport:NoQuestionText");
        problem.Column.ShouldBe(QuestionCsvParser.QuestionColumnKey);
    }

    [Fact]
    public void Multiple_choice_is_refused_as_ambiguous_rather_than_guessed_at()
    {
        // It means one answer to most of the English-speaking world and several
        // to the rest of it. Guessing produces a bank that grades wrongly and
        // looks right, which is the worst outcome available here.
        var problem = _parser.Read(EnglishHeader + "\n" + "multiple choice,Pick,A,B,C,D,1,1,,").Rows.Single();

        problem.Reason.ShouldBe("IMS:QuestionImport:AmbiguousType");
        problem.Column.ShouldBe(QuestionCsvParser.TypeColumnKey);
    }

    [Fact]
    public void A_type_nobody_here_knows_is_reported_against_the_type_column()
    {
        var problem = _parser.Read(EnglishHeader + "\n" + "crossword,Pick,A,B,C,D,1,1,,").Rows.Single();

        problem.Reason.ShouldBe("IMS:QuestionImport:UnknownType");
        problem.Column.ShouldBe(QuestionCsvParser.TypeColumnKey);
    }

    [Fact]
    public void An_unclear_true_or_false_answer_is_reported_rather_than_assumed()
    {
        // Assuming "false" would silently invert a question, and nothing about the
        // saved question would say so.
        _parser.Read(EnglishHeader + "\n" + "true or false,Is it?,,,,,maybe,1,,")
            .Rows.Single().Reason.ShouldBe("IMS:QuestionImport:TrueFalseAnswerUnclear");
    }

    [Fact]
    public void Marks_that_are_not_a_number_are_reported_against_the_marks_column()
    {
        var problem = _parser.Read(EnglishHeader + "\n" + "single choice,Q,A,B,C,D,1,two,,").Rows.Single();

        problem.Reason.ShouldBe("IMS:QuestionImport:MarksNotANumber");
        problem.Column.ShouldBe(QuestionCsvParser.MarksColumnKey);
    }

    [Fact]
    public void Marks_outside_what_a_question_may_carry_are_reported()
    {
        _parser.Read(EnglishHeader + "\n" + "single choice,Q,A,B,C,D,1,0,,")
            .Rows.Single().Reason.ShouldBe("IMS:QuestionImport:MarksOutOfRange");
    }

    [Fact]
    public void A_difficulty_nobody_here_knows_is_reported_rather_than_defaulted()
    {
        // Quietly filing a question as medium when the author wrote something
        // else means a blueprint draws a paper they did not design.
        var problem = _parser.Read(EnglishHeader + "\n" + "single choice,Q,A,B,C,D,1,1,tricky,").Rows.Single();

        problem.Reason.ShouldBe("IMS:QuestionImport:UnknownDifficulty");
        problem.Column.ShouldBe(QuestionCsvParser.DifficultyColumnKey);
    }

    // ---------------------------------------------- what goes wrong with the file

    [Fact]
    public void A_file_with_no_question_column_is_refused_outright()
    {
        // Refused as a file, not as four hundred identical rows. Reporting each
        // of them would bury the one sentence that helps.
        var thrown = Should.Throw<BusinessException>(() =>
            _parser.Read("Type,Notes\nsingle choice,whatever"));

        thrown.Code.ShouldBe("IMS:QuestionImport:NoQuestionColumn");
    }

    [Fact]
    public void A_file_with_no_type_column_is_refused_rather_than_guessed_at()
    {
        // Four option columns could be single choice or several answers, and
        // guessing wrong produces a bank that grades incorrectly and looks fine.
        var thrown = Should.Throw<BusinessException>(() =>
            _parser.Read("Question,Option 1,Option 2\nWhat?,A,B"));

        thrown.Code.ShouldBe("IMS:QuestionImport:NoTypeColumn");
    }

    [Fact]
    public void A_file_with_headings_and_nothing_under_them_is_refused()
    {
        Should.Throw<BusinessException>(() => _parser.Read(EnglishHeader))
            .Code.ShouldBe("IMS:QuestionImport:NoRows");
    }

    [Fact]
    public void An_empty_file_is_refused()
    {
        Should.Throw<BusinessException>(() => _parser.Read(Array.Empty<byte>()))
            .Code.ShouldBe("IMS:QuestionImport:FileEmpty");
    }

    [Fact]
    public void Columns_this_does_not_recognise_are_ignored_rather_than_refused()
    {
        // A sheet exported from somewhere else carries columns of its own, and
        // refusing the file over them would be refusing a usable bank.
        var sheet = _parser.Read(
            "Reference,Type,Author,Question,Option 1,Option 2,Correct answer\n" +
            "REF-4,single choice,Layla,Two plus two?,3,4,2");

        Only(sheet).Options.Single(o => o.IsCorrect).Text.ShouldBe("4");
    }

    // ------------------------------------------------------------- the payload

    [Fact]
    public void The_payload_a_row_produces_is_one_the_grader_actually_scores()
    {
        // The parser and the grader are the two halves of this feature and
        // nothing structural connects them. A payload that reads correctly and
        // grades to zero is the failure this whole path exists to avoid, and it
        // would only surface while somebody is sitting the exam.
        var draft = Only(_parser.Read(
            EnglishHeader + "\n" +
            "single choice,What is the capital of Egypt?,Cairo,Alexandria,Aswan,Tanta,1,2,,"));

        var payload = QuestionCsvParser.PayloadFor(draft);
        var options = PayloadJson.Read<ChoicePayload>(payload)!.Options;

        var correct = options.Single(o => o.IsCorrect).Id;

        new SingleChoiceGrader().Grade(payload, correct, 2m).AwardedScore.ShouldBe(2m);
        new SingleChoiceGrader().Grade(payload, options.First(o => !o.IsCorrect).Id, 2m)
            .AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void A_short_answer_payload_accepts_every_spelling_the_author_listed()
    {
        var draft = Only(_parser.Read(
            EnglishHeader + "\n" +
            "short answer,Name the currency of Egypt.,,,,,\"pound|Egyptian pound\",2,,"));

        var payload = QuestionCsvParser.PayloadFor(draft);
        var blankId = PayloadJson.Read<FillInTheBlankPayload>(payload)!.Blanks.Single().Id;

        var grader = new FillInTheBlankGrader();

        // Case is not what is being measured, and an import is exactly where
        // nobody is around to reconsider it afterwards.
        grader.Grade(payload, PayloadJson.Write(new Dictionary<string, string> { [blankId] = "POUND" }), 2m)
            .AwardedScore.ShouldBe(2m);

        grader.Grade(payload, PayloadJson.Write(new Dictionary<string, string> { [blankId] = "dinar" }), 2m)
            .AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void Option_identifiers_are_generated_rather_than_asked_for()
    {
        // An id is what a stored answer refers to after the options have been
        // shuffled. Asking an author to invent stable identifiers in a
        // spreadsheet column would be asking them to hold a programmer's concern.
        var draft = Only(_parser.Read(EnglishHeader + "\n" + "single choice,Q,A,B,C,D,1,1,,"));

        var options = PayloadJson.Read<ChoicePayload>(QuestionCsvParser.PayloadFor(draft))!.Options;

        options.Select(o => o.Id).ShouldBe(new[] { "o1", "o2", "o3", "o4" });
        options.Select(o => o.Id).Distinct().Count().ShouldBe(4);
    }

    // ------------------------------------------------------------------ helpers

    private static QuestionDraft Only(QuestionSheet sheet)
    {
        var row = sheet.Rows.ShouldHaveSingleItem();

        row.Reason.ShouldBeNull($"row {row.Line} was refused: {row.Reason}");

        return row.Question!;
    }

    private static string Quote(string value) =>
        value.Contains(',') ? '"' + value.Replace("\"", "\"\"") + '"' : value;

    /// <summary>
    /// The real localisation file, read rather than copied.
    /// <para>
    /// A copy drifts, and the way it fails is quietly: the test keeps passing
    /// against headings nobody ships any more.
    /// </para>
    /// </summary>
    private static Dictionary<string, string> TextsFor(string language)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("could not find the solution above the test binary");

        var path = Path.Combine(
            directory!.FullName,
            "src",
            "InternshipManagementSystem.Domain.Shared",
            "Localization",
            "InternshipManagementSystem",
            $"{language}.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement
            .GetProperty("texts")
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
    }
}
