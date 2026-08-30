using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.Assessment.Review.Dtos;

/// <summary>An attempt waiting on a human, as it appears in the queue.</summary>
public class ReviewQueueItemDto
{
    public Guid AttemptId { get; set; }
    public string CandidateName { get; set; } = default!;
    public string ExamTitle { get; set; } = default!;

    public DateTime SubmittedAt { get; set; }

    /// <summary>How many answers still need a mark, so a reviewer can plan.</summary>
    public int PendingCount { get; set; }

    /// <summary>Marks already awarded automatically.</summary>
    public decimal ProvisionalScore { get; set; }
    public decimal MaxScore { get; set; }

    /// <summary>
    /// Behavioural observations recorded during the attempt. A count, not a verdict:
    /// it tells the reviewer where to look, and nothing more.
    /// </summary>
    public int IntegrityFlagCount { get; set; }
}

/// <summary>One answer as the reviewer sees it — including everything the taker never got.</summary>
public class ReviewAnswerDto
{
    public Guid AnswerId { get; set; }
    public Guid QuestionId { get; set; }

    public string QuestionText { get; set; } = default!;
    public string QuestionType { get; set; } = default!;
    public decimal MaxScore { get; set; }

    public string? Response { get; set; }

    /// <summary>Time-limited URL for an uploaded file or recording.</summary>
    public string? AnswerFileUrl { get; set; }
    public string? AnswerFileName { get; set; }

    /// <summary>
    /// The rubric to mark against. Two reviewers scoring the same answer out of ten
    /// will disagree; scoring it against named criteria they mostly will not — and a
    /// candidate who disputes a mark can be shown why they got it.
    /// </summary>
    public List<RubricCriterionDto> Rubric { get; set; } = new();

    /// <summary>Guidance written for the marker, which the taker never saw.</summary>
    public string? ReviewerGuidance { get; set; }

    /// <summary>The key, so the reviewer is not marking blind.</summary>
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }

    // Already-recorded review, when this answer has been marked before.
    public decimal? AwardedScore { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Behavioural context for this specific answer.
    public bool WasPasted { get; set; }
    public int? TimeSpentSeconds { get; set; }
    public int KeystrokeCount { get; set; }
    public int BackspaceCount { get; set; }
}

public class RubricCriterionDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
}

/// <summary>A reviewer's mark for one answer.</summary>
public class GradeAnswerDto
{
    [Required]
    public Guid AnswerId { get; set; }

    /// <summary>Total awarded. Must not exceed the question's maximum.</summary>
    [Range(0, 9999)]
    public decimal AwardedScore { get; set; }

    /// <summary>Per-criterion marks when a rubric was used, keyed by criterion id.</summary>
    public Dictionary<string, decimal>? RubricScores { get; set; }

    /// <summary>Why. Shown to the candidate, so it is feedback rather than a bare number.</summary>
    [MaxLength(4000)]
    public string? Comment { get; set; }
}

/// <summary>What an attempt's integrity signals amount to. Advisory, always.</summary>
public class IntegrityReportDto
{
    public Guid AttemptId { get; set; }

    public List<IntegritySignalDto> Signals { get; set; } = new();

    /// <summary>
    /// A short human summary of what was observed. Deliberately descriptive rather
    /// than a score or a judgement: the system reports behaviour, a person decides
    /// what it means.
    /// </summary>
    public List<string> Observations { get; set; } = new();
}

public class IntegritySignalDto
{
    public IntegritySignalType Type { get; set; }
    public Guid? QuestionId { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? Magnitude { get; set; }
}

/// <summary>
/// Which sittings the marker wants to see.
/// <para>
/// The queue's job is what is waiting, and that stays the default. But an
/// attempt left it the moment its last answer was marked and never came back, so
/// a marker who typed 7 where they meant 17 had no route to the sitting at all —
/// the queue no longer listed it, and nothing else does.
/// </para>
/// <para>
/// A mark is a person's judgement and people revise judgements. Making the
/// revision impossible does not make the first mark more correct; it makes it
/// permanent, which is a different thing.
/// </para>
/// </summary>
public class ReviewQueueRequestDto : Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto
{
    /// <summary>Sittings already marked, newest first, instead of those waiting.</summary>
    public bool Finished { get; set; }
}
