using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace InternshipManagementSystem.Assessment.Grading.Graders;

/// <summary>Shared option handling for the choice family.</summary>
public abstract class ChoiceGraderBase : IQuestionGrader
{
    public abstract string QuestionType { get; }

    public abstract GradeResult Grade(string payload, string? response, decimal maxScore);

    /// <summary>
    /// Reads the selected option ids. Accepts a JSON array, and also a bare string
    /// so a single-choice answer can be stored without ceremony.
    /// </summary>
    protected static HashSet<string> ReadSelection(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var trimmed = response.Trim();

        if (trimmed.StartsWith('['))
        {
            var ids = PayloadJson.Read<List<string>>(trimmed);
            return ids is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        }

        if (trimmed.StartsWith('"'))
        {
            try
            {
                var single = JsonSerializer.Deserialize<string>(trimmed, PayloadJson.Options);
                return single is null
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(new[] { single }, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        return new HashSet<string>(new[] { trimmed }, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>Exactly one option is correct.</summary>
public class SingleChoiceGrader : ChoiceGraderBase
{
    public override string QuestionType => QuestionTypes.SingleChoice;

    public override GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<ChoicePayload>(payload);
        if (spec is null || spec.Options.Count == 0)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        var selected = ReadSelection(response);
        if (selected.Count != 1)
        {
            return GradeResult.Wrong();
        }

        var correctId = spec.Options.FirstOrDefault(o => o.IsCorrect)?.Id;
        if (correctId is null)
        {
            return GradeResult.Manual("Question has no correct option marked.");
        }

        return selected.Contains(correctId) ? GradeResult.Correct(maxScore) : GradeResult.Wrong();
    }
}

/// <summary>True or false. Same rules as single choice, kept separate so the UI can differ.</summary>
public class TrueFalseGrader : SingleChoiceGrader
{
    public override string QuestionType => QuestionTypes.TrueFalse;
}

/// <summary>
/// Several options may be correct.
/// <para>
/// <b>A single wrong pick scores zero.</b> The previous implementation scored
/// <c>correct ∩ selected / total correct</c> and never looked at wrong picks, so a
/// taker who ticked every box scored full marks on every multi-select question in
/// the system. Partial credit now rewards incomplete-but-correct answers only.
/// </para>
/// </summary>
public class MultiSelectGrader : ChoiceGraderBase
{
    public override string QuestionType => QuestionTypes.MultiSelect;

    public override GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<ChoicePayload>(payload);
        if (spec is null || spec.Options.Count == 0)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        var selected = ReadSelection(response);
        if (selected.Count == 0)
        {
            return GradeResult.Wrong();
        }

        var correctIds = spec.Options.Where(o => o.IsCorrect).Select(o => o.Id)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (correctIds.Count == 0)
        {
            return GradeResult.Manual("Question has no correct options marked.");
        }

        // Any incorrect selection voids the question. Without this, selecting
        // everything guarantees full marks.
        if (selected.Any(id => !correctIds.Contains(id)))
        {
            return GradeResult.Wrong();
        }

        var hits = selected.Count(id => correctIds.Contains(id));

        if (hits == correctIds.Count)
        {
            return GradeResult.Correct(maxScore);
        }

        if (!spec.AllowPartialCredit)
        {
            return GradeResult.Wrong();
        }

        var awarded = Math.Round(maxScore * hits / correctIds.Count, 2, MidpointRounding.AwayFromZero);
        return GradeResult.Partial(awarded, maxScore);
    }
}
