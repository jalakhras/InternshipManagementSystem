using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// A behavioural observation recorded during an attempt: a paste, a window losing
/// focus, an answer arriving faster than it could be written.
/// <para>
/// These are shown to a human reviewer and never act on their own. That is a
/// deliberate limit, not a missing feature. Text-based AI detectors are unreliable
/// and misfire hardest on people writing in a second language, so accusing someone
/// on their output is indefensible; how the text reached the page is observable
/// fact, and the judgement stays with a person.
/// </para>
/// </summary>
public class IntegritySignal : AuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AttemptId { get; set; }

    /// <summary>Which question was on screen, when one was.</summary>
    public Guid? QuestionId { get; set; }

    public IntegritySignalType Type { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>Magnitude: characters pasted, seconds away from the window, and so on.</summary>
    public int? Magnitude { get; set; }

    /// <summary>Anything extra worth keeping, as JSON.</summary>
    public string? Detail { get; set; }

    protected IntegritySignal() { }

    public IntegritySignal(Guid id, Guid? tenantId, Guid attemptId, IntegritySignalType type, DateTime occurredAt) : base(id)
    {
        TenantId = tenantId;
        AttemptId = attemptId;
        Type = type;
        OccurredAt = occurredAt;
    }
}
