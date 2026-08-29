using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// A decision to give an exam to someone: one person, or a whole group in a single
/// action. Fanning an assignment out produces one <see cref="ExamLink"/> per person,
/// each with its own token, so links stay individually traceable and revocable.
/// </summary>
public class Assignment : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    /// <summary>
    /// The named paper this sitting uses, or null to draw one per candidate.
    /// <para>
    /// This is where a form becomes real. It was briefly configured on the class
    /// instead, which read well and did nothing: no code consumed it when an
    /// attempt started, so a coordinator could nominate Form 2 for the retake and
    /// every student would still receive a random draw.
    /// </para>
    /// <para>
    /// On the sitting rather than the class because a sitting is what a form
    /// belongs to — the morning group and the afternoon group are one class and
    /// two papers, and a resit is a second sitting rather than a second class.
    /// </para>
    /// </summary>
    public Guid? ExamFormId { get; set; }

    /// <summary>
    /// Move to the next published paper on each retake.
    /// <para>
    /// The reason named forms exist at all, made automatic. Without it a
    /// coordinator has to remember, at the moment they send a resit, that this
    /// person already sat Form 1 — and if they forget, the retake measures what
    /// the candidate remembers of the first go rather than what they know.
    /// </para>
    /// <para>
    /// Ignored when <see cref="ExamFormId"/> names a paper: an explicit choice is
    /// an explicit choice, and silently overriding it would be worse than not
    /// offering the option.
    /// </para>
    /// </summary>
    public bool RotateForms { get; set; }

    /// <summary>Set for a single-person assignment.</summary>
    public Guid? CandidateId { get; set; }

    /// <summary>Set for a whole-group assignment. Exactly one of the two is set.</summary>
    public Guid? CandidateGroupId { get; set; }

    /// <summary>When the links stop working.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Attempts allowed per person.</summary>
    public int MaxAttempts { get; set; } = 1;

    /// <summary>Email the link on creation.</summary>
    public bool SendEmail { get; set; } = true;

    /// <summary>How many links this assignment produced.</summary>
    public int LinkCount { get; set; }

    /// <summary>How many of those emails were sent successfully. The rest are reported back.</summary>
    public int EmailsSent { get; set; }

    public string? Note { get; set; }

    protected Assignment() { }

    public Assignment(Guid id, Guid? tenantId, Guid examId, DateTime expiresAt) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        ExpiresAt = expiresAt;
    }
}
