using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// One person's sitting of one exam.
/// <para>
/// The deadline is stored, not derived on the client, and every save returns the
/// remaining time from it. The browser's clock is never trusted, and a closed
/// browser cannot leave an attempt open forever — a background worker submits
/// anything past its deadline.
/// </para>
/// </summary>
public class Attempt : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid? ExamLinkId { get; set; }

    /// <summary>
    /// The named paper this attempt was served, or null when it was drawn.
    /// <para>
    /// Recorded rather than inferred. A result only means the same thing as
    /// another if the papers behind them are known, and a form can be retired
    /// after somebody sat it — so which paper this was has to be written down at
    /// the time.
    /// </para>
    /// </summary>
    public Guid? ExamFormId { get; set; }

    public DateTime StartedAt { get; set; }

    /// <summary>The moment this attempt stops being accepted. The single source of truth for time.</summary>
    public DateTime DeadlineAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public AttemptEndReason EndReason { get; set; } = AttemptEndReason.None;

    public bool IsSubmitted { get; set; }

    /// <summary>Auto-grading has run and no question is waiting on a human.</summary>
    public bool IsGraded { get; set; }

    /// <summary>At least one answer needs a human before the result is final.</summary>
    public bool NeedsManualReview { get; set; }

    /// <summary>Marks earned so far. Recomputed whenever a manual review is saved.</summary>
    public decimal Score { get; set; }

    /// <summary>Marks available on this taker's form. Forms differ, so this is per attempt.</summary>
    public decimal MaxScore { get; set; }

    /// <summary>Score as a share of <see cref="MaxScore"/>, which is what the pass mark compares against.</summary>
    public decimal ScorePercentage { get; set; }

    public bool IsPassed { get; set; }

    /// <summary>
    /// Seed for shuffling this attempt's questions and options. Persisted so a reload
    /// produces the same order — a shuffle that changes under the taker is a bug, not a defence.
    /// </summary>
    public int ShuffleSeed { get; set; }

    /// <summary>Count of integrity observations, surfaced to the reviewer. Never an automatic verdict.</summary>
    public int IntegrityFlagCount { get; set; }

    /// <summary>
    /// Why a person ended this sitting, when a person did.
    /// <para>
    /// Recorded because ending somebody's exam early is the kind of act that gets
    /// questioned weeks later — by the candidate, by an auditor, by the
    /// coordinator's own manager — and "the system did it" is not an answer
    /// anybody can defend. Null on every attempt that ended by itself.
    /// </para>
    /// </summary>
    public string? EndedByReason { get; set; }

    public ICollection<AttemptQuestion> Questions { get; set; } = new List<AttemptQuestion>();
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();

    protected Attempt() { }

    public Attempt(Guid id, Guid? tenantId, Guid examId, Guid candidateId,
                   DateTime startedAt, DateTime deadlineAt, int shuffleSeed) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        CandidateId = candidateId;
        StartedAt = startedAt;
        DeadlineAt = deadlineAt;
        ShuffleSeed = shuffleSeed;
    }

    public int SecondsRemaining(DateTime now)
    {
        var remaining = (int)(DeadlineAt - now).TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }

    public bool IsExpired(DateTime now) => now > DeadlineAt;

    /// <summary>
    /// Applies a freshly computed total. Passing is a percentage comparison, so an
    /// exam worth 200 marks is not judged against a threshold meant for one worth 100.
    /// </summary>
    public void ApplyScore(decimal score, decimal maxScore, decimal passingPercentage)
    {
        Score = score;
        MaxScore = maxScore;
        ScorePercentage = maxScore > 0 ? Math.Round(score / maxScore * 100m, 2) : 0m;
        IsPassed = ScorePercentage >= passingPercentage;
    }
}
