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
