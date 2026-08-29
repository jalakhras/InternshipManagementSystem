using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InternshipManagementSystem.Assessment.Grading.Graders;

/// <summary>
/// A number accepted within a tolerance. Position sizing, engineering, chemistry —
/// anywhere the right answer is a quantity rather than a choice.
/// </summary>
public class NumericGrader : IQuestionGrader
{
    /// <summary>
    /// The furthest an answer can sit from the key before the difference itself
    /// overflows. Decimal arithmetic throws rather than saturating, and the input
    /// is a string a candidate typed.
    /// </summary>
    private const decimal MaxComparable = 1_000_000_000_000_000m;

    public string QuestionType => QuestionTypes.Numeric;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<NumericPayload>(payload);
        if (spec is null)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            return GradeResult.Wrong();
        }

        var cleaned = response.Trim().Trim('"').Replace(",", string.Empty);

        if (!decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var given))
        {
            return GradeResult.Wrong();
        }

        var tolerance = Math.Abs(spec.Tolerance);

        // Subtracted inside a checked comparison rather than directly. Unlike
        // double, decimal throws on overflow, and decimal.MinValue is something a
        // candidate can type into an answer box: the subtraction would raise, the
        // submission would roll back, and the attempt would end up unsubmittable
        // and then force-closed ungraded. An answer that cannot be within the
        // tolerance of anything is simply wrong.
        if (given < spec.CorrectValue - MaxComparable || given > spec.CorrectValue + MaxComparable)
        {
            return GradeResult.Wrong();
        }

        return Math.Abs(given - spec.CorrectValue) <= tolerance
            ? GradeResult.Correct(maxScore)
            : GradeResult.Wrong();
    }
}

/// <summary>Pair each left item with its right item.</summary>
public class MatchingGrader : IQuestionGrader
{
    public string QuestionType => QuestionTypes.Matching;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<MatchingPayload>(payload);
        if (spec is null || spec.Pairs.Count == 0)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        // Response is { leftId: rightId, ... }
        var given = PayloadJson.Read<Dictionary<string, string>>(response);
        if (given is null || given.Count == 0)
        {
            return GradeResult.Wrong();
        }

        var hits = spec.Pairs.Count(p =>
            given.TryGetValue(p.LeftId, out var chosen) &&
            string.Equals(chosen, p.RightId, StringComparison.OrdinalIgnoreCase));

        if (hits == spec.Pairs.Count)
        {
            return GradeResult.Correct(maxScore);
        }

        if (!spec.AllowPartialCredit)
        {
            return GradeResult.Wrong();
        }

        var awarded = Math.Round(maxScore * hits / spec.Pairs.Count, 2, MidpointRounding.AwayFromZero);
        return GradeResult.Partial(awarded, maxScore);
    }
}

/// <summary>Put the items in the correct sequence. Procedures, workflows, safety steps.</summary>
public class OrderingGrader : IQuestionGrader
{
    public string QuestionType => QuestionTypes.Ordering;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<OrderingPayload>(payload);
        if (spec is null || spec.Items.Count == 0)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        // Response is an ordered array of item ids.
        var given = PayloadJson.Read<List<string>>(response);
        if (given is null || given.Count == 0)
        {
            return GradeResult.Wrong();
        }

        var hits = spec.Items.Count(item =>
        {
            var index = given.FindIndex(id => string.Equals(id, item.Id, StringComparison.OrdinalIgnoreCase));
            return index == item.CorrectPosition;
        });

        if (hits == spec.Items.Count)
        {
            return GradeResult.Correct(maxScore);
        }

        if (!spec.AllowPartialCredit)
        {
            return GradeResult.Wrong();
        }

        var awarded = Math.Round(maxScore * hits / spec.Items.Count, 2, MidpointRounding.AwayFromZero);
        return GradeResult.Partial(awarded, maxScore);
    }
}

/// <summary>Click the right region of an image. Charts, anatomy, diagrams, maps.</summary>
public class HotspotGrader : IQuestionGrader
{
    public string QuestionType => QuestionTypes.Hotspot;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<HotspotPayload>(payload);
        if (spec is null || spec.Regions.Count == 0)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        // Response is { x, y } as percentages of the image, so it scales with any render size.
        var point = PayloadJson.Read<HotspotAnswer>(response);
        if (point is null)
        {
            return GradeResult.Wrong();
        }

        var hit = spec.Regions.FirstOrDefault(r =>
            point.X >= r.X && point.X <= r.X + r.Width &&
            point.Y >= r.Y && point.Y <= r.Y + r.Height);

        return hit is { IsCorrect: true } ? GradeResult.Correct(maxScore) : GradeResult.Wrong();
    }

    private sealed class HotspotAnswer
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
    }
}

/// <summary>Fill the blanks. Each blank accepts any of its listed answers, so synonyms count.</summary>
public class FillInTheBlankGrader : IQuestionGrader
{
    public string QuestionType => QuestionTypes.FillInTheBlank;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<FillInTheBlankPayload>(payload);
        if (spec is null || spec.Blanks.Count == 0)
        {
            return GradeResult.Manual("Question payload could not be read.");
        }

        var given = PayloadJson.Read<Dictionary<string, string>>(response);
        if (given is null || given.Count == 0)
        {
            return GradeResult.Wrong();
        }

        var comparison = spec.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var hits = spec.Blanks.Count(b =>
            given.TryGetValue(b.Id, out var typed) &&
            !string.IsNullOrWhiteSpace(typed) &&
            b.AcceptedAnswers.Any(a => string.Equals(a.Trim(), typed.Trim(), comparison)));

        if (hits == spec.Blanks.Count)
        {
            return GradeResult.Correct(maxScore);
        }

        if (!spec.AllowPartialCredit)
        {
            return GradeResult.Wrong();
        }

        var awarded = Math.Round(maxScore * hits / spec.Blanks.Count, 2, MidpointRounding.AwayFromZero);
        return GradeResult.Partial(awarded, maxScore);
    }
}

/// <summary>
/// Compares submitted output with expected output as text. The code is not executed —
/// that is a stated constraint, and a real execution sandbox is a separate project.
/// </summary>
public class CodeOutputGrader : IQuestionGrader
{
    public string QuestionType => QuestionTypes.Code;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
    {
        var spec = PayloadJson.Read<CodePayload>(payload);
        if (spec is null || string.IsNullOrWhiteSpace(spec.ExpectedOutput))
        {
            return GradeResult.Manual("No expected output set; needs a human.");
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            return GradeResult.Wrong();
        }

        return Normalise(response) == Normalise(spec.ExpectedOutput)
            ? GradeResult.Correct(maxScore)
            : GradeResult.Wrong();
    }

    /// <summary>Line endings and trailing whitespace are not part of the answer.</summary>
    private static string Normalise(string value)
    {
        var lines = value.Replace("\r\n", "\n")
                         .Replace('\r', '\n')
                         .Split('\n')
                         .Select(line => line.TrimEnd());

        return string.Join('\n', lines).Trim();
    }
}
