using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// Reads a spreadsheet of questions.
/// <para>
/// The single largest thing standing between an exam author and using this
/// product is that their question bank is already in a spreadsheet. Retyping
/// eighty questions, four options each, is the reason authoring stops on the
/// first evening — the same reason the candidate import exists, one screen over.
/// </para>
/// <para>
/// Everything here is bent around one rule: <b>no cell may require programming
/// skill</b>. The payload a question stores is JSON, and asking somebody to type
/// JSON into a spreadsheet cell would be asking them to be a programmer. So the
/// sheet carries an option per column and a correct-answer column holding a
/// number, a letter, or the answer written out — and this class is what turns
/// that into a payload the graders read.
/// </para>
/// <para>
/// It is deliberately free of the database and of the localiser: it takes bytes
/// and returns rows, which is what lets the whole vocabulary — every accepted
/// header, every accepted type word, every accepted way of saying "the second
/// one" — be tested without a host.
/// </para>
/// </summary>
public class QuestionCsvParser : ITransientDependency
{
    /// <summary>
    /// How many options one question may carry.
    /// <para>
    /// Six because a sheet is written by hand and a seventh column is nearly
    /// always somebody's notes rather than an option. Extra option columns are
    /// read; this only caps what one row may use.
    /// </para>
    /// </summary>
    public const int MaxOptions = 10;

    /// <summary>
    /// Rows past this are refused as a file rather than as rows.
    /// <para>
    /// A dry run projects every row into memory and reports on it. Somebody who
    /// picked the wrong file should be told so, not made to wait while a
    /// hundred-thousand-line export is turned into a preview nobody can read.
    /// </para>
    /// </summary>
    public const int MaxRows = 2000;

    // The localisation keys naming each column, used both by the error reporting
    // here and by the template the author downloads. One source of truth, so the
    // file we hand out and the file we can read cannot drift apart.
    public const string TypeColumnKey = "QuestionImport:Column:Type";
    public const string QuestionColumnKey = "QuestionImport:Column:Question";
    public const string OptionColumnKey = "QuestionImport:Column:Option";

    /// <summary>
    /// Names the option columns as a group, for a problem that is about all of
    /// them rather than one. Distinct from the heading above, which carries the
    /// column number and would report every such problem as "Option 1".
    /// </summary>
    public const string OptionsColumnKey = "QuestionImport:Column:Options";
    public const string CorrectColumnKey = "QuestionImport:Column:Correct";
    public const string MarksColumnKey = "QuestionImport:Column:Marks";
    public const string DifficultyColumnKey = "QuestionImport:Column:Difficulty";
    public const string ExplanationColumnKey = "QuestionImport:Column:Explanation";

    /// <summary>
    /// Reads the file as the author's spreadsheet wrote it.
    /// <para>
    /// Excel's "CSV UTF-8" writes a byte-order mark, and a mark left in place
    /// lands on the first header cell — so the Type column is not found, and the
    /// author is told their file has no columns while looking at a file that
    /// plainly does. <see cref="StreamReader"/> semantics are reproduced here by
    /// hand rather than taken on trust: the mark is stripped whether it arrived
    /// as bytes or as a character some other client already decoded.
    /// </para>
    /// </summary>
    public QuestionSheet Read(byte[] content)
    {
        if (content is null || content.Length == 0)
        {
            throw new BusinessException("IMS:QuestionImport:FileEmpty");
        }

        // UTF-8 with the mark stripped. Deliberately not encoding detection
        // beyond that: a file saved as Windows-1256 would decode into confident
        // nonsense, and nonsense the author can see beats a silent half-import.
        var text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            .GetString(content);

        return Read(text);
    }

    /// <summary>Reads an already-decoded sheet. The entry point the unit tests use.</summary>
    public QuestionSheet Read(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            throw new BusinessException("IMS:QuestionImport:FileEmpty");
        }

        // A mark that survived somebody else's decoding. Same damage, one layer up.
        csv = csv.TrimStart('﻿');

        var records = ReadRecords(csv);

        if (records.Count == 0)
        {
            throw new BusinessException("IMS:QuestionImport:FileEmpty");
        }

        var header = records[0];
        var map = MapHeader(header.Cells);

        if (map.TextIndex < 0)
        {
            // Refused as a file, not as rows. Without a question column there is
            // nothing to report per row, and reporting four hundred identical
            // problems would bury the one sentence that helps.
            throw new BusinessException("IMS:QuestionImport:NoQuestionColumn");
        }

        if (map.TypeIndex < 0)
        {
            // Not guessed from the shape of the row. A sheet with four option
            // columns could be single choice or several answers, and guessing
            // wrong produces a bank that grades incorrectly and looks fine.
            throw new BusinessException("IMS:QuestionImport:NoTypeColumn");
        }

        if (records.Count - 1 > MaxRows)
        {
            throw new BusinessException("IMS:QuestionImport:TooManyRows")
                .WithData("Max", MaxRows);
        }

        var sheet = new QuestionSheet();

        foreach (var record in records.Skip(1))
        {
            // A row of empty cells is what a spreadsheet leaves behind under the
            // last real row. Silently skipped: reporting it as a problem would
            // put a red line under a file that is perfectly fine.
            if (record.Cells.All(cell => cell.Length == 0))
            {
                continue;
            }

            sheet.Rows.Add(ReadRow(record, map));
        }

        if (sheet.Rows.Count == 0)
        {
            throw new BusinessException("IMS:QuestionImport:NoRows");
        }

        return sheet;
    }

    // --------------------------------------------------------------- one row

    private static QuestionRow ReadRow(Record record, HeaderMap map)
    {
        var line = record.Line;
        var content = Summarise(record.Cells);

        string Cell(int index) =>
            index >= 0 && index < record.Cells.Count ? record.Cells[index].Trim() : string.Empty;

        QuestionRow Problem(string reason, string column) => new()
        {
            Line = line,
            Content = content,
            Reason = reason,
            Column = column,
        };

        var text = Cell(map.TextIndex);

        if (text.Length == 0)
        {
            return Problem("IMS:QuestionImport:NoQuestionText", QuestionColumnKey);
        }

        var type = ResolveType(Cell(map.TypeIndex));

        if (type is null)
        {
            // Two different messages on purpose. "I have never heard of this word"
            // and "this word means two different things to two different people"
            // send an author to different places, and the second one is the trap:
            // "multiple choice" means one answer to half the world and several to
            // the other half.
            return Problem(
                Ambiguous.Contains(Normalise(Cell(map.TypeIndex)))
                    ? "IMS:QuestionImport:AmbiguousType"
                    : "IMS:QuestionImport:UnknownType",
                TypeColumnKey);
        }

        var score = 1m;

        if (Cell(map.ScoreIndex) is { Length: > 0 } rawScore)
        {
            if (!TryReadDecimal(rawScore, out score))
            {
                return Problem("IMS:QuestionImport:MarksNotANumber", MarksColumnKey);
            }

            if (score is <= 0m or > 1000m)
            {
                return Problem("IMS:QuestionImport:MarksOutOfRange", MarksColumnKey);
            }
        }

        var difficulty = QuestionDifficulty.Medium;

        if (Cell(map.DifficultyIndex) is { Length: > 0 } rawDifficulty)
        {
            if (ResolveDifficulty(rawDifficulty) is not { } resolved)
            {
                return Problem("IMS:QuestionImport:UnknownDifficulty", DifficultyColumnKey);
            }

            difficulty = resolved;
        }

        var draft = new QuestionDraft
        {
            Type = type,
            Text = text,
            Score = score,
            Difficulty = difficulty,
            Explanation = Cell(map.ExplanationIndex) is { Length: > 0 } note ? note : null,
        };

        var answer = Cell(map.CorrectIndex);

        var problem = type switch
        {
            QuestionTypes.TrueFalse => FillTrueFalse(draft, answer),
            QuestionTypes.Text or QuestionTypes.FillInTheBlank => FillWritten(draft, answer),
            _ => FillChoice(draft, answer, map.OptionIndexes.Select(Cell).ToList()),
        };

        if (problem is not null)
        {
            return Problem(problem.Value.Reason, problem.Value.Column);
        }

        return new QuestionRow { Line = line, Content = content, Question = draft };
    }

    // ------------------------------------------------------------- the types

    /// <summary>
    /// Single choice and multiple answers: options in their own columns, and the
    /// correct one named by number, by letter, or written out.
    /// </summary>
    private static (string Reason, string Column)? FillChoice(
        QuestionDraft draft,
        string answer,
        IReadOnlyList<string> optionCells)
    {
        var options = optionCells
            .Select(cell => cell.Trim())
            .Where(cell => cell.Length > 0)
            .Take(MaxOptions)
            .ToList();

        if (options.Count < 2)
        {
            // The same code the payload validator raises, so an author sees one
            // sentence for one mistake whether they typed the question or imported it.
            return ("IMS:Question:NeedsTwoOptions", OptionsColumnKey);
        }

        if (options.Select(Normalise).Distinct().Count() != options.Count)
        {
            // Two identical options make one of them unpickable and the key
            // ambiguous. It reads as a typo in the sheet, which is what it is.
            return ("IMS:QuestionImport:RepeatedOption", OptionsColumnKey);
        }

        if (answer.Length == 0)
        {
            return ("IMS:Question:NoCorrectOption", CorrectColumnKey);
        }

        var picked = ResolveAnswers(answer, options);

        if (picked is null)
        {
            return ("IMS:QuestionImport:AnswerIsNotOneOfTheOptions", CorrectColumnKey);
        }

        if (draft.Type == QuestionTypes.SingleChoice && picked.Count != 1)
        {
            // The single-choice grader wants exactly one selection, so a key
            // naming two would fail every candidate. Caught here rather than at
            // save so the row number and the column come with it.
            return ("IMS:Question:SingleChoiceHasManyCorrect", CorrectColumnKey);
        }

        if (draft.Type == QuestionTypes.MultiSelect && picked.Count == options.Count)
        {
            // Every option correct means ticking everything is right, which
            // measures nothing.
            return ("IMS:Question:AllOptionsCorrect", CorrectColumnKey);
        }

        for (var index = 0; index < options.Count; index++)
        {
            draft.Options.Add(new DraftOption
            {
                Text = options[index],
                IsCorrect = picked.Contains(index),
            });
        }

        return null;
    }

    /// <summary>
    /// True or false. The two options are not columns in the sheet — nobody
    /// should have to type "True" and "False" eighty times — so they are written
    /// here, in the language the author answered in.
    /// </summary>
    private static (string Reason, string Column)? FillTrueFalse(QuestionDraft draft, string answer)
    {
        var normalised = Normalise(answer);

        if (normalised.Length == 0)
        {
            return ("IMS:Question:NoCorrectOption", CorrectColumnKey);
        }

        bool isTrue;

        if (TrueWords.Contains(normalised))
        {
            isTrue = true;
        }
        else if (FalseWords.Contains(normalised))
        {
            isTrue = false;
        }
        else
        {
            return ("IMS:QuestionImport:TrueFalseAnswerUnclear", CorrectColumnKey);
        }

        // The pair reads in whichever language the author answered in. A sheet
        // of Arabic questions whose options say "True" and "False" looks like
        // somebody else's exam pasted into theirs.
        var arabic = ArabicWords.Contains(normalised);

        draft.Options.Add(new DraftOption { Text = arabic ? "صح" : "True", IsCorrect = isTrue });
        draft.Options.Add(new DraftOption { Text = arabic ? "خطأ" : "False", IsCorrect = !isTrue });

        return null;
    }

    /// <summary>
    /// A short written answer.
    /// <para>
    /// Two questions, not one, and the correct-answer cell decides which. With
    /// answers listed it becomes a fill-in-the-blank with a single blank, which a
    /// machine marks. Left empty it becomes a written answer, which a person
    /// marks. Both are legitimate and an author means one of them — so the type
    /// follows what they wrote rather than a setting they would have to find.
    /// </para>
    /// </summary>
    private static (string Reason, string Column)? FillWritten(QuestionDraft draft, string answer)
    {
        var accepted = Split(answer)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accepted.Count == 0)
        {
            draft.Type = QuestionTypes.Text;

            return null;
        }

        draft.Type = QuestionTypes.FillInTheBlank;
        draft.AcceptedAnswers.AddRange(accepted);

        return null;
    }

    // ----------------------------------------------------------- the payload

    /// <summary>
    /// Turns a row into the JSON the graders read.
    /// <para>
    /// Option ids are generated rather than taken from the sheet. An id is what
    /// a stored answer refers to after the options have been shuffled, and asking
    /// an author to invent stable identifiers is asking them to hold a
    /// programmer's concern.
    /// </para>
    /// </summary>
    public static string PayloadFor(QuestionDraft draft)
    {
        if (draft.Type == QuestionTypes.FillInTheBlank)
        {
            return Grading.PayloadJson.Write(new Grading.FillInTheBlankPayload
            {
                Blanks =
                [
                    new Grading.BlankSpec { Id = "b1", AcceptedAnswers = draft.AcceptedAnswers.ToList() },
                ],

                // Capital letters do not decide whether somebody knew the answer,
                // and an import is exactly where nobody is around to reconsider it.
                CaseSensitive = false,
            });
        }

        if (draft.Type == QuestionTypes.Text)
        {
            // No rubric. A rubric is a table of its own and does not belong in a
            // question row; the author adds one afterwards where there is room.
            return "{}";
        }

        return Grading.PayloadJson.Write(new Grading.ChoicePayload
        {
            Options = draft.Options
                .Select((option, index) => new Grading.OptionPayload
                {
                    Id = $"o{index + 1}",
                    Text = option.Text,
                    IsCorrect = option.IsCorrect,
                })
                .ToList(),

            // Partial credit on multiple answers, because a candidate who found
            // two of the three correct options knew more than one who found none —
            // and a wrong pick still voids the question, so it cannot be gamed.
            AllowPartialCredit = draft.Type == QuestionTypes.MultiSelect,
        });
    }

    // --------------------------------------------------------------- reading

    /// <summary>
    /// Splits the file into records.
    /// <para>
    /// Quotes are honoured, because a question with a comma in it is the normal
    /// case and a sheet that loses half of every prompt would be worse than no
    /// import at all. The record's line number is where it <em>started</em>, so a
    /// prompt containing a line break still reports the row the author is looking
    /// at in their spreadsheet.
    /// </para>
    /// </summary>
    private static List<Record> ReadRecords(string csv)
    {
        var delimiter = DetectDelimiter(csv);

        var records = new List<Record>();
        var cells = new List<string>();
        var cell = new StringBuilder();

        var quoted = false;
        var line = 1;
        var recordLine = 1;

        void EndCell()
        {
            cells.Add(cell.ToString());
            cell.Clear();
        }

        void EndRecord()
        {
            EndCell();
            records.Add(new Record(recordLine, cells.ToList()));
            cells.Clear();
        }

        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];

            if (quoted)
            {
                if (character == '"')
                {
                    // A doubled quote is a literal one. The alternative reading —
                    // that the field ended and another began without a delimiter —
                    // is not something any spreadsheet writes.
                    if (index + 1 < csv.Length && csv[index + 1] == '"')
                    {
                        cell.Append('"');
                        index++;

                        continue;
                    }

                    quoted = false;

                    continue;
                }

                if (character == '\n')
                {
                    line++;
                }

                cell.Append(character);

                continue;
            }

            if (character == '"' && cell.Length == 0)
            {
                quoted = true;

                continue;
            }

            if (character == delimiter)
            {
                EndCell();

                continue;
            }

            if (character is '\r' or '\n')
            {
                // \r\n counts once. Counting it twice would put every reported
                // row number one further out than the last.
                if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                {
                    index++;
                }

                EndRecord();

                line++;
                recordLine = line;

                continue;
            }

            cell.Append(character);
        }

        if (cell.Length > 0 || cells.Count > 0)
        {
            EndRecord();
        }

        // Trailing blank records: a file ending in a newline, and the rows a
        // spreadsheet leaves under the last real one.
        return records
            .Where(record => record.Cells.Any(value => value.Trim().Length > 0))
            .ToList();
    }

    /// <summary>
    /// Which character separates the columns.
    /// <para>
    /// Asking the author would be asking them to know something about their own
    /// file that nothing on their screen tells them. Excel writes a semicolon
    /// wherever the system decimal separator is a comma — most of Europe, and
    /// several Arabic locales — and a tab is what a paste out of a sheet carries.
    /// Counted over the header alone, which is the one line guaranteed not to
    /// contain prose.
    /// </para>
    /// </summary>
    private static char DetectDelimiter(string csv)
    {
        var firstBreak = csv.IndexOfAny(['\r', '\n']);
        var header = firstBreak < 0 ? csv : csv[..firstBreak];

        var counts = new[] { ',', ';', '\t' }
            .Select(candidate => (Candidate: candidate, Count: CountOutsideQuotes(header, candidate)))
            .OrderByDescending(pair => pair.Count)
            .ToList();

        return counts[0].Count == 0 ? ',' : counts[0].Candidate;
    }

    private static int CountOutsideQuotes(string line, char character)
    {
        var quoted = false;
        var count = 0;

        foreach (var current in line)
        {
            if (current == '"')
            {
                quoted = !quoted;
            }
            else if (!quoted && current == character)
            {
                count++;
            }
        }

        return count;
    }

    // ---------------------------------------------------------- the headings

    /// <summary>
    /// Works out which column is which.
    /// <para>
    /// Every heading is matched after normalisation, so a column headed
    /// <c>الإجابة الصحيحة</c>, <c>الاجابه الصحيحه</c> or <c>Correct Answer</c> is
    /// the same column. Headings nobody here recognises are ignored rather than
    /// refused: a sheet exported from somewhere else carries columns of its own,
    /// and refusing the file over them would be refusing a usable bank.
    /// </para>
    /// </summary>
    private static HeaderMap MapHeader(IReadOnlyList<string> cells)
    {
        var map = new HeaderMap();

        for (var index = 0; index < cells.Count; index++)
        {
            var heading = Normalise(cells[index]);

            if (heading.Length == 0)
            {
                continue;
            }

            if (OptionNumber(heading) is not null)
            {
                map.OptionIndexes.Add(index);

                continue;
            }

            if (TypeHeadings.Contains(heading)) { map.TypeIndex = Prefer(map.TypeIndex, index); continue; }
            if (TextHeadings.Contains(heading)) { map.TextIndex = Prefer(map.TextIndex, index); continue; }
            if (CorrectHeadings.Contains(heading)) { map.CorrectIndex = Prefer(map.CorrectIndex, index); continue; }
            if (ScoreHeadings.Contains(heading)) { map.ScoreIndex = Prefer(map.ScoreIndex, index); continue; }
            if (DifficultyHeadings.Contains(heading)) { map.DifficultyIndex = Prefer(map.DifficultyIndex, index); continue; }
            if (ExplanationHeadings.Contains(heading)) { map.ExplanationIndex = Prefer(map.ExplanationIndex, index); }
        }

        return map;
    }

    /// <summary>First one wins, so a duplicated heading does not silently move a column.</summary>
    private static int Prefer(int existing, int candidate) => existing < 0 ? candidate : existing;

    /// <summary>
    /// Whether a heading names an option column, and which one.
    /// <para>
    /// The number is read but not relied on for ordering: the columns are taken
    /// in the order they appear, because a sheet whose columns run 1, 2, 4 means
    /// three options rather than a hole where the third should be.
    /// </para>
    /// </summary>
    private static int? OptionNumber(string heading)
    {
        foreach (var word in OptionHeadings)
        {
            if (!heading.StartsWith(word, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = heading[word.Length..].Trim();

            if (rest.Length == 0)
            {
                return 0;
            }

            if (int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }
        }

        return null;
    }

    // -------------------------------------------------------------- the words

    /// <summary>
    /// Which question type a word means, or null when nobody here knows.
    /// <para>
    /// A dictionary rather than a fuzzy match. A guess that lands on the wrong
    /// type produces a question that grades wrongly and looks fine, which is the
    /// failure this whole class exists to avoid.
    /// </para>
    /// </summary>
    public static string? ResolveType(string value)
    {
        var normalised = Normalise(value);

        return normalised.Length == 0 ? null : TypeWords.GetValueOrDefault(normalised);
    }

    private static QuestionDifficulty? ResolveDifficulty(string value)
    {
        var normalised = Normalise(value);

        return DifficultyWords.TryGetValue(normalised, out var difficulty) ? difficulty : null;
    }

    /// <summary>
    /// Reads the correct-answer cell against the options actually on the row.
    /// <para>
    /// Three ways of saying it, because three are what people write: the option's
    /// number, its letter, or the answer itself. Refusing two of them would make
    /// the sheet a format to learn rather than a table to fill in — and the
    /// author who writes the answer out is the one who is <em>least</em> likely to
    /// key it to the wrong row.
    /// </para>
    /// <para>
    /// Returns null when something in the cell matches nothing, rather than
    /// quietly keeping the parts that did. A key that silently loses one of its
    /// two correct answers is a question that marks a right answer wrong.
    /// </para>
    /// </summary>
    private static HashSet<int>? ResolveAnswers(string cell, IReadOnlyList<string> options)
    {
        // Tried whole before split, so an option whose own text contains a comma
        // can still be named by writing it out.
        if (MatchOption(cell, options) is { } whole)
        {
            return [whole];
        }

        var picked = new HashSet<int>();

        foreach (var token in Split(cell))
        {
            var trimmed = token.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            if (MatchOption(trimmed, options) is not { } index)
            {
                return null;
            }

            picked.Add(index);
        }

        return picked.Count == 0 ? null : picked;
    }

    private static int? MatchOption(string token, IReadOnlyList<string> options)
    {
        var normalised = Normalise(token);

        if (normalised.Length == 0)
        {
            return null;
        }

        if (int.TryParse(normalised, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
            number >= 1 && number <= options.Count)
        {
            return number - 1;
        }

        if (normalised.Length == 1)
        {
            var letters = OptionLetters.IndexOf(normalised[0]);

            if (letters >= 0 && letters < options.Count)
            {
                return letters;
            }

            var arabic = ArabicOptionLetters.IndexOf(normalised[0]);

            if (arabic >= 0 && arabic < options.Count)
            {
                return arabic;
            }
        }

        for (var index = 0; index < options.Count; index++)
        {
            if (Normalise(options[index]) == normalised)
            {
                return index;
            }
        }

        return null;
    }

    /// <summary>
    /// Splits a cell that may list several things.
    /// <para>
    /// Every separator a person might reach for, including the Arabic comma and
    /// semicolon — which is what an Arabic keyboard produces and what a rule
    /// written for the Latin ones would have thrown away.
    /// </para>
    /// </summary>
    private static IEnumerable<string> Split(string value) =>
        value.Split([',', '،', ';', '؛', '|', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Reads a number a person typed.
    /// <para>
    /// Deliberately not <see cref="Normalise"/>: that turns a full stop into a
    /// space, because in a heading it separates words — and it would read 2.5
    /// marks as the two words "2" and "5". Only the digits and the separator are
    /// translated here.
    /// </para>
    /// </summary>
    private static bool TryReadDecimal(string value, out decimal result)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            var mapped = character;

            if (mapped >= '٠' && mapped <= '٩')
            {
                mapped = (char)('0' + (mapped - '٠'));
            }
            else if (mapped >= '۰' && mapped <= '۹')
            {
                mapped = (char)('0' + (mapped - '۰'));
            }
            else if (mapped is '٫' or ',')
            {
                // The Arabic decimal separator, and the comma half of Europe
                // writes one with.
                mapped = '.';
            }

            builder.Append(mapped);
        }

        return decimal.TryParse(
            builder.ToString(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
    }

    /// <summary>
    /// One spelling for many.
    /// <para>
    /// Arabic is written with optional vowel marks, three spellings of alef, two
    /// of the final ha, and two sets of digits — none of which change the word.
    /// Matching the raw text would mean an author whose keyboard produced
    /// <c>الإجابة</c> and one whose produced <c>الاجابه</c> could not use the same
    /// sheet, and neither would ever find out why.
    /// </para>
    /// </summary>
    public static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSpace = false;

        foreach (var character in value.Trim())
        {
            var mapped = character switch
            {
                // Alef, in all its spellings.
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',

                // Teh marbuta and alef maqsura, which people type either way.
                'ة' => 'ه',
                'ى' => 'ي',
                'ؤ' => 'و',
                'ئ' => 'ي',

                // Punctuation that separates words in a heading rather than
                // belonging to one.
                '-' or '_' or '/' or '\\' or '.' or ':' or '(' or ')' => ' ',

                _ => character,
            };

            // Harakat, tatweel and the superscript alef: decoration, never meaning.
            if (mapped is 'ـ' or 'ٰ' || (mapped >= 'ً' && mapped <= 'ٟ'))
            {
                continue;
            }

            // Both sets of Arabic digits, read as the numbers they are.
            if (mapped >= '٠' && mapped <= '٩')
            {
                mapped = (char)('0' + (mapped - '٠'));
            }
            else if (mapped >= '۰' && mapped <= '۹')
            {
                mapped = (char)('0' + (mapped - '۰'));
            }

            if (char.IsWhiteSpace(mapped))
            {
                if (builder.Length > 0 && !lastWasSpace)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(mapped));
            lastWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>The row as one readable line, for the problem list beside the row number.</summary>
    private static string Summarise(IEnumerable<string> cells)
    {
        var joined = string.Join(" | ", cells.Select(cell => cell.Trim()).Where(cell => cell.Length > 0));

        return joined.Length <= 160 ? joined : joined[..160] + "…";
    }

    // ------------------------------------------------------------ vocabulary

    private const string OptionLetters = "abcdefghij";
    private const string ArabicOptionLetters = "ابجدهوزحطي";

    private static readonly string[] OptionHeadings =
    [
        "خيار", "الخيار", "اختيار", "الاختيار", "بديل", "البديل",
        "option", "choice", "answer option", "opt",
    ];

    private static readonly HashSet<string> TypeHeadings = new(StringComparer.Ordinal)
    {
        "النوع", "نوع", "نوع السوال", "نوع الاسئله",
        "type", "question type", "kind", "format",
    };

    private static readonly HashSet<string> TextHeadings = new(StringComparer.Ordinal)
    {
        "السوال", "سوال", "نص السوال", "الاسئله", "المتن",
        "question", "question text", "text", "prompt", "the question", "stem",
    };

    private static readonly HashSet<string> CorrectHeadings = new(StringComparer.Ordinal)
    {
        "الاجابه الصحيحه", "الاجابه", "الجواب", "الجواب الصحيح", "الصحيح",
        "الاجابات الصحيحه", "المفتاح",
        "correct", "correct answer", "correct answers", "answer", "the answer", "key", "right answer",
    };

    private static readonly HashSet<string> ScoreHeadings = new(StringComparer.Ordinal)
    {
        "الدرجه", "الدرجات", "درجه", "العلامه", "النقاط",
        "marks", "mark", "score", "points", "point", "weight",
    };

    private static readonly HashSet<string> DifficultyHeadings = new(StringComparer.Ordinal)
    {
        "الصعوبه", "صعوبه", "مستوى الصعوبه", "درجه الصعوبه",
        "difficulty", "level", "hardness",
    };

    private static readonly HashSet<string> ExplanationHeadings = new(StringComparer.Ordinal)
    {
        "التفسير", "الشرح", "شرح", "التعليل", "السبب",
        "explanation", "rationale", "why", "feedback", "note",
    };

    /// <summary>
    /// Words that name two different things to two different people.
    /// <para>
    /// "Multiple choice" means one correct answer to most of the English-speaking
    /// world and several to the rest of it. Guessing would produce a bank that
    /// grades wrongly and looks right, so the author is asked which they meant.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> Ambiguous = new(StringComparer.Ordinal)
    {
        "multiple choice", "multiple", "choice", "mcq",
    };

    private static readonly Dictionary<string, string> TypeWords = new(StringComparer.Ordinal)
    {
        // One answer out of several.
        ["اختيار واحد"] = QuestionTypes.SingleChoice,
        ["اختيار من متعدد"] = QuestionTypes.SingleChoice,
        ["اختيار من عده"] = QuestionTypes.SingleChoice,
        ["اجابه واحده"] = QuestionTypes.SingleChoice,
        ["اختيار"] = QuestionTypes.SingleChoice,
        ["single"] = QuestionTypes.SingleChoice,
        ["single choice"] = QuestionTypes.SingleChoice,
        ["single answer"] = QuestionTypes.SingleChoice,
        ["one answer"] = QuestionTypes.SingleChoice,
        ["choose one"] = QuestionTypes.SingleChoice,
        ["radio"] = QuestionTypes.SingleChoice,
        ["mcq single"] = QuestionTypes.SingleChoice,

        // Several answers.
        ["اختيار متعدد"] = QuestionTypes.MultiSelect,
        ["اختيارات متعدده"] = QuestionTypes.MultiSelect,
        ["اجابات متعدده"] = QuestionTypes.MultiSelect,
        ["اكثر من اجابه"] = QuestionTypes.MultiSelect,
        ["متعدد"] = QuestionTypes.MultiSelect,
        ["متعدده"] = QuestionTypes.MultiSelect,
        ["multi"] = QuestionTypes.MultiSelect,
        ["multi select"] = QuestionTypes.MultiSelect,
        ["multiple answers"] = QuestionTypes.MultiSelect,
        ["multiple correct"] = QuestionTypes.MultiSelect,
        ["choose many"] = QuestionTypes.MultiSelect,
        ["select all"] = QuestionTypes.MultiSelect,
        ["checkbox"] = QuestionTypes.MultiSelect,
        ["multi choice"] = QuestionTypes.MultiSelect,

        // True or false.
        ["صح او خطا"] = QuestionTypes.TrueFalse,
        ["صح ام خطا"] = QuestionTypes.TrueFalse,
        ["صح خطا"] = QuestionTypes.TrueFalse,
        ["صواب او خطا"] = QuestionTypes.TrueFalse,
        ["صواب وخطا"] = QuestionTypes.TrueFalse,
        ["صح وخطا"] = QuestionTypes.TrueFalse,
        ["صح"] = QuestionTypes.TrueFalse,
        ["true false"] = QuestionTypes.TrueFalse,
        ["true or false"] = QuestionTypes.TrueFalse,
        ["truefalse"] = QuestionTypes.TrueFalse,
        ["tf"] = QuestionTypes.TrueFalse,
        ["boolean"] = QuestionTypes.TrueFalse,
        ["yes no"] = QuestionTypes.TrueFalse,
        ["true/false"] = QuestionTypes.TrueFalse,

        // A short written answer. Whether a machine or a person marks it is
        // decided by the correct-answer cell — see FillWritten.
        ["اجابه قصيره"] = QuestionTypes.Text,
        ["اجابه نصيه"] = QuestionTypes.Text,
        ["نص"] = QuestionTypes.Text,
        ["نصي"] = QuestionTypes.Text,
        ["كتابه"] = QuestionTypes.Text,
        ["مقالي"] = QuestionTypes.Text,
        ["short text"] = QuestionTypes.Text,
        ["short answer"] = QuestionTypes.Text,
        ["free text"] = QuestionTypes.Text,
        ["written"] = QuestionTypes.Text,
        ["write"] = QuestionTypes.Text,
        ["essay"] = QuestionTypes.Text,
        ["open"] = QuestionTypes.Text,
        ["text"] = QuestionTypes.Text,
        ["fill in the blank"] = QuestionTypes.Text,
    };

    private static readonly Dictionary<string, QuestionDifficulty> DifficultyWords = new(StringComparer.Ordinal)
    {
        ["سهل"] = QuestionDifficulty.Easy,
        ["سهله"] = QuestionDifficulty.Easy,
        ["بسيط"] = QuestionDifficulty.Easy,
        ["easy"] = QuestionDifficulty.Easy,
        ["low"] = QuestionDifficulty.Easy,
        ["simple"] = QuestionDifficulty.Easy,

        ["متوسط"] = QuestionDifficulty.Medium,
        ["متوسطه"] = QuestionDifficulty.Medium,
        ["medium"] = QuestionDifficulty.Medium,
        ["normal"] = QuestionDifficulty.Medium,
        ["average"] = QuestionDifficulty.Medium,
        ["moderate"] = QuestionDifficulty.Medium,

        ["صعب"] = QuestionDifficulty.Hard,
        ["صعبه"] = QuestionDifficulty.Hard,
        ["hard"] = QuestionDifficulty.Hard,
        ["difficult"] = QuestionDifficulty.Hard,
        ["high"] = QuestionDifficulty.Hard,
    };

    private static readonly HashSet<string> TrueWords = new(StringComparer.Ordinal)
    {
        "صح", "صحيح", "صحيحه", "صواب", "نعم",
        "true", "t", "yes", "y", "1",
    };

    private static readonly HashSet<string> FalseWords = new(StringComparer.Ordinal)
    {
        "خطا", "خاطي", "خاطيه", "غلط", "لا",
        "false", "f", "no", "n", "0",
    };

    /// <summary>
    /// Answers written in Arabic, so the two options this generates are written
    /// in Arabic too.
    /// </summary>
    private static readonly HashSet<string> ArabicWords = new(StringComparer.Ordinal)
    {
        "صح", "صحيح", "صحيحه", "صواب", "نعم", "خطا", "خاطي", "خاطيه", "غلط", "لا",
    };

    private sealed record Record(int Line, List<string> Cells);

    private sealed class HeaderMap
    {
        public int TypeIndex = -1;
        public int TextIndex = -1;
        public int CorrectIndex = -1;
        public int ScoreIndex = -1;
        public int DifficultyIndex = -1;
        public int ExplanationIndex = -1;

        public List<int> OptionIndexes { get; } = [];
    }
}

/// <summary>A read sheet: every row, whether it worked or not.</summary>
public sealed class QuestionSheet
{
    public List<QuestionRow> Rows { get; } = [];
}

/// <summary>
/// One row of the sheet, read: either a question or the reason it is not one.
/// <para>
/// Never both, and never neither. A row that half-worked is how an import writes
/// a question whose key is missing.
/// </para>
/// </summary>
public sealed class QuestionRow
{
    /// <summary>
    /// One-based and counted over the file, so it is the row number the author
    /// is looking at in their spreadsheet — the header being row 1.
    /// </summary>
    public int Line { get; init; }

    /// <summary>The row as written, so somebody can recognise it without opening the file.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>A localisation key. Null when the row read cleanly.</summary>
    public string? Reason { get; init; }

    /// <summary>A localisation key naming the column at fault, so the fix is one cell.</summary>
    public string? Column { get; init; }

    public QuestionDraft? Question { get; init; }
}

/// <summary>What one row says, before any of it has been written down.</summary>
public sealed class QuestionDraft
{
    public string Type { get; set; } = default!;

    public string Text { get; init; } = default!;

    public decimal Score { get; init; } = 1m;

    public QuestionDifficulty Difficulty { get; init; } = QuestionDifficulty.Medium;

    public string? Explanation { get; init; }

    /// <summary>Options for the choice family, in the order the columns ran.</summary>
    public List<DraftOption> Options { get; } = [];

    /// <summary>Every spelling a short written answer accepts.</summary>
    public List<string> AcceptedAnswers { get; } = [];
}

public sealed class DraftOption
{
    public string Text { get; init; } = default!;

    public bool IsCorrect { get; init; }
}
