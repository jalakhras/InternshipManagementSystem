using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Typed views over <c>Question.Payload</c>. Each question type reads only the
/// shape it needs, so types stay independent of one another.
/// </summary>
public static class PayloadJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? Read<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            // A malformed payload must not take an exam down: the caller falls
            // back to manual review, and the reviewer sees the question as-is.
            return null;
        }
    }

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
}

/// <summary>One selectable option.</summary>
public sealed class OptionPayload
{
    public string Id { get; set; } = default!;
    public string Text { get; set; } = default!;
    public bool IsCorrect { get; set; }

    /// <summary>Blob name when the option is an image rather than text.</summary>
    public string? BlobName { get; set; }
}

/// <summary>Payload for single-choice, multi-select and true/false.</summary>
public sealed class ChoicePayload
{
    public List<OptionPayload> Options { get; set; } = new();

    /// <summary>
    /// Multi-select only. When false, any selection short of exactly right scores zero.
    /// When true, correct picks earn their share — but a single wrong pick still
    /// scores zero, otherwise selecting everything would score full marks.
    /// </summary>
    public bool AllowPartialCredit { get; set; }
}

/// <summary>Payload for a numeric answer accepted within a tolerance.</summary>
public sealed class NumericPayload
{
    public decimal CorrectValue { get; set; }

    /// <summary>Absolute tolerance. 0.5 accepts anything within ±0.5.</summary>
    public decimal Tolerance { get; set; }

    public string? Unit { get; set; }
}

/// <summary>Payload for matching left items to right items.</summary>
public sealed class MatchingPayload
{
    public List<MatchingPair> Pairs { get; set; } = new();
    public bool AllowPartialCredit { get; set; } = true;
}

public sealed class MatchingPair
{
    public string LeftId { get; set; } = default!;
    public string LeftText { get; set; } = default!;
    public string RightId { get; set; } = default!;
    public string RightText { get; set; } = default!;
}

/// <summary>Payload for putting items into the correct sequence.</summary>
public sealed class OrderingPayload
{
    public List<OrderingItem> Items { get; set; } = new();

    /// <summary>Award marks for each item in its right place, rather than all-or-nothing.</summary>
    public bool AllowPartialCredit { get; set; } = true;
}

public sealed class OrderingItem
{
    public string Id { get; set; } = default!;
    public string Text { get; set; } = default!;

    /// <summary>Zero-based correct position.</summary>
    public int CorrectPosition { get; set; }
}

/// <summary>Payload for clicking the right region of an image.</summary>
public sealed class HotspotPayload
{
    public string ImageBlobName { get; set; } = default!;
    public List<HotspotRegion> Regions { get; set; } = new();
}

/// <summary>An accepted region, in percentages of image width and height so it scales.</summary>
public sealed class HotspotRegion
{
    public string Id { get; set; } = default!;
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public bool IsCorrect { get; set; }
    public string? Label { get; set; }
}

/// <summary>Payload for filling blanks in a sentence.</summary>
public sealed class FillInTheBlankPayload
{
    public List<BlankSpec> Blanks { get; set; } = new();
    public bool CaseSensitive { get; set; }
    public bool AllowPartialCredit { get; set; } = true;
}

public sealed class BlankSpec
{
    public string Id { get; set; } = default!;

    /// <summary>Any of these is accepted, to allow synonyms and spelling variants.</summary>
    public List<string> AcceptedAnswers { get; set; } = new();
}

/// <summary>
/// Payload for a code question. Output is compared as text, not executed —
/// a documented constraint, and the reason a real execution engine is a later step.
/// </summary>
public sealed class CodePayload
{
    public string? Language { get; set; }
    public string? StarterTemplate { get; set; }
    public string? ExpectedOutput { get; set; }
}

/// <summary>Payload for a rubric-graded question: free text, file upload, audio.</summary>
public sealed class RubricPayload
{
    public List<RubricCriterion> Criteria { get; set; } = new();

    /// <summary>Guidance shown to the reviewer, not to the taker.</summary>
    public string? ReviewerGuidance { get; set; }
}

/// <summary>
/// One line a reviewer scores. Rubrics are what make two reviewers reach the same
/// mark, and what lets a tenant defend a grade to someone who disputes it.
/// </summary>
public sealed class RubricCriterion
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
}

/// <summary>Payload for an agree/disagree scale.</summary>
public sealed class ScalePayload
{
    public int Min { get; set; } = 1;
    public int Max { get; set; } = 5;
    public string? MinLabel { get; set; }
    public string? MaxLabel { get; set; }
}
