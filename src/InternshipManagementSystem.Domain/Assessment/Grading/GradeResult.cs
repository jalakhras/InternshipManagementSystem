namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>What a grader concluded about one answer.</summary>
public sealed class GradeResult
{
    /// <summary>Marks awarded.</summary>
    public decimal AwardedScore { get; init; }

    /// <summary>Null when correctness is not a meaningful yes/no for this type.</summary>
    public bool? IsCorrect { get; init; }

    /// <summary>The grader declined to decide; a human must.</summary>
    public bool NeedsManualReview { get; init; }

    /// <summary>Optional note explaining the award, kept for audit.</summary>
    public string? Note { get; init; }

    public static GradeResult Correct(decimal score) =>
        new() { AwardedScore = score, IsCorrect = true };

    public static GradeResult Wrong() =>
        new() { AwardedScore = 0m, IsCorrect = false };

    public static GradeResult Partial(decimal score, decimal maxScore) =>
        new() { AwardedScore = score, IsCorrect = score >= maxScore };

    public static GradeResult Manual(string? note = null) =>
        new() { AwardedScore = 0m, IsCorrect = null, NeedsManualReview = true, Note = note };
}
