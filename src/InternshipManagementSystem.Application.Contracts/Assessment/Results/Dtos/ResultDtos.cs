using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Assessment.Results.Dtos;

/// <summary>
/// One person's sitting, as the coordinator sees it in a list.
/// </summary>
public class ResultRowDto
{
    public Guid AttemptId { get; set; }

    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = default!;
    public string CandidateEmail { get; set; } = default!;

    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = default!;

    /// <summary>The named paper, when the sitting used one. Blank when it was drawn.</summary>
    public string? FormName { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public bool IsSubmitted { get; set; }

    /// <summary>
    /// False while something on the paper still needs a person to mark it.
    /// <para>
    /// Shown rather than hidden. A coordinator looking for a missing result needs
    /// to know it is waiting on the review queue and not on the candidate.
    /// </para>
    /// </summary>
    public bool IsGraded { get; set; }

    public bool NeedsManualReview { get; set; }

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal ScorePercentage { get; set; }

    public bool IsPassed { get; set; }

    /// <summary>How the sitting ended: submitted, timed out, abandoned.</summary>
    public string EndReason { get; set; } = default!;

    /// <summary>Signals worth a second look, counted rather than listed here.</summary>
    public int IntegrityFlagCount { get; set; }

    /// <summary>Minutes from start to submission, or to the deadline when it was never submitted.</summary>
    public int DurationInMinutes { get; set; }
}

public class ResultListRequestDto : PagedAndSortedResultRequestDto
{
    public Guid? ExamId { get; set; }
    public Guid? CandidateGroupId { get; set; }

    /// <summary>Narrows to one paper, so two forms of one exam can be compared.</summary>
    public Guid? ExamFormId { get; set; }

    /// <summary>Name or email.</summary>
    public string? Filter { get; set; }

    public bool? PassedOnly { get; set; }

    /// <summary>Only sittings still waiting on a person to mark them.</summary>
    public bool? AwaitingMarking { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>
/// The header figures above a roster.
/// <para>
/// Computed over the whole filtered set rather than the page, because "68% passed"
/// is a statement about the cohort and a page-sized version of it would be a
/// different number every time somebody turned a page.
/// </para>
/// </summary>
public class ResultSummaryDto
{
    public int Sat { get; set; }

    /// <summary>Sent a link and never started. The number a coordinator chases.</summary>
    public int NotStarted { get; set; }

    public int Passed { get; set; }
    public int Failed { get; set; }

    /// <summary>Still waiting on a person to mark something.</summary>
    public int AwaitingMarking { get; set; }

    public decimal AverageScorePercentage { get; set; }
    public decimal HighestScorePercentage { get; set; }
    public decimal LowestScorePercentage { get; set; }

    /// <summary>The middle score, which says more than the mean when a few people walked out.</summary>
    public decimal MedianScorePercentage { get; set; }
}

/// <summary>One sitting in full: every question, what was given, what it earned.</summary>
public class ResultDetailDto
{
    public ResultRowDto Summary { get; set; } = default!;

    public List<ResultAnswerDto> Answers { get; set; } = new();

    /// <summary>
    /// The score broken down by topic.
    /// <para>
    /// The reason a result is worth more than a number: "strong on grammar, weak
    /// on listening" is something a training centre can act on, where 64% is not.
    /// Empty when the questions carry no topic.
    /// </para>
    /// </summary>
    public List<TopicScoreDto> ByTopic { get; set; } = new();
}

public class ResultAnswerDto
{
    public Guid QuestionId { get; set; }

    public int Position { get; set; }

    public string QuestionText { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? TopicName { get; set; }

    /// <summary>What the candidate gave, as stored. Null when they left it.</summary>
    public string? Response { get; set; }

    public string? AnswerFileName { get; set; }

    public bool? IsCorrect { get; set; }

    public decimal AwardedScore { get; set; }
    public decimal MaxScore { get; set; }

    public bool NeedsManualReview { get; set; }
    public string? ReviewComment { get; set; }

    public int? TimeSpentSeconds { get; set; }
}

public class TopicScoreDto
{
    public Guid? TopicId { get; set; }

    /// <summary>The catalogue name, or a stand-in when the questions carry no topic.</summary>
    public string TopicName { get; set; } = default!;

    public int QuestionCount { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal ScorePercentage { get; set; }
}

/// <summary>
/// How each question behaved, over every attempt that has answered it.
/// <para>
/// This is what stops a bank rotting. A question everybody gets right measures
/// nothing; a question the strongest candidates get wrong more often than the
/// weakest is either mis-keyed or badly worded, and no amount of reading it will
/// show that as reliably as the number does.
/// </para>
/// </summary>
public class ItemAnalysisRowDto
{
    public Guid QuestionId { get; set; }

    public string Text { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? TopicName { get; set; }

    /// <summary>How many attempts have answered it. Below about thirty, read the rest with suspicion.</summary>
    public int TimesAnswered { get; set; }

    /// <summary>
    /// The proportion who got it right, which the literature calls difficulty and
    /// which runs backwards: 0.95 is an easy question.
    /// </summary>
    public decimal Facility { get; set; }

    /// <summary>
    /// Whether this question separates the strong from the weak, as the difference
    /// in facility between the top and bottom quarter of candidates by total score.
    /// <para>
    /// Above 0.3 is healthy. Near zero means it tells you nothing. Negative means
    /// the better candidates got it wrong more often, which nearly always means a
    /// wrong answer key.
    /// </para>
    /// </summary>
    public decimal Discrimination { get; set; }

    /// <summary>Set when the numbers say something worth acting on.</summary>
    public string? FlagKey { get; set; }
}
