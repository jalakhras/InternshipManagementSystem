using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// What one taker gave for one question.
/// <para>
/// Written on every autosave, not once at submit: a dropped connection late in an
/// exam must not cost someone the work they had already done.
/// </para>
/// </summary>
public class Answer : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>
    /// The response, as JSON shaped by the question type: a chosen option id, a set
    /// of ids, a number, a pairing map, a hotspot coordinate, free text.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>Blob name when the answer is an uploaded file or a recording.</summary>
    public string? AnswerBlobName { get; set; }

    /// <summary>Name the taker's file had, for the reviewer's benefit.</summary>
    public string? AnswerFileName { get; set; }

    /// <summary>Null until graded — some types cannot be judged automatically at all.</summary>
    public bool? IsCorrect { get; set; }

    /// <summary>Marks awarded. Decimal so partial credit survives.</summary>
    public decimal? AwardedScore { get; set; }

    /// <summary>Set when this answer is waiting on a human.</summary>
    public bool NeedsManualReview { get; set; }

    // ---- Manual review ----

    public string? ReviewComment { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Per-criterion marks when a rubric was used, as JSON. Makes a grade explainable.</summary>
    public string? RubricScores { get; set; }

    // ---- Behavioural context, gathered while answering ----

    /// <summary>Seconds spent on this question.</summary>
    public int? TimeSpentSeconds { get; set; }

    /// <summary>Whether the response arrived by paste. The single strongest cheating signal.</summary>
    public bool WasPasted { get; set; }

    /// <summary>Keystrokes recorded. Far below the response length means it was not typed here.</summary>
    public int KeystrokeCount { get; set; }

    /// <summary>Corrections made. Zero across a long answer is not how people write.</summary>
    public int BackspaceCount { get; set; }

    public DateTime? AnsweredAt { get; set; }

    protected Answer() { }

    public Answer(Guid id, Guid? tenantId, Guid attemptId, Guid questionId) : base(id)
    {
        TenantId = tenantId;
        AttemptId = attemptId;
        QuestionId = questionId;
    }
}
