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

    /// <summary>
    /// Turns a total weight into marks, floored at zero and capped at the question's.
    /// <para>
    /// Floored because a scoring system that can leave a taker worse off than not
    /// answering teaches candidates not to answer, which measures their nerve
    /// rather than their knowledge. A harmful option costs a taker the marks they
    /// could have had; it does not take marks they earned elsewhere.
    /// </para>
    /// </summary>
    protected static GradeResult Award(decimal totalWeight, decimal maxScore)
    {
        var awarded = Math.Round(maxScore * totalWeight, 2, MidpointRounding.AwayFromZero);

        awarded = Math.Clamp(awarded, 0m, maxScore);

        return GradeResult.Partial(awarded, maxScore);
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

        if (spec.Weighted == true)
        {
            // The chosen option is worth what the author priced it at. An option
            // the payload does not know is worth nothing rather than a failure:
            // a stale id in a stored answer must not make the question unscoreable.
            var chosen = spec.Options.FirstOrDefault(o => selected.Contains(o.Id));

            return Award(chosen?.Weight ?? 0m, maxScore);
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

        if (spec.Weighted == true)
        {
            // Weights add up. The all-or-nothing rule below does not apply here
            // and must not: a harmful option is already priced below zero, which
            // is how weighted mode closes the same "tick everything" hole that
            // rule was written to close. The validator refuses a weighted
            // multi-select where nothing is priced below zero, so the two
            // defences never both go missing.
            var total = spec.Options
                .Where(o => selected.Contains(o.Id))
                .Sum(o => o.Weight ?? 0m);

            return Award(total, maxScore);
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
