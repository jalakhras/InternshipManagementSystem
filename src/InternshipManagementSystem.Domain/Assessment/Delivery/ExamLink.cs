using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// One person's way into one exam.
/// <para>
/// The token is stored hashed, never in the clear: a leaked database backup should
/// not hand over working exam links. The plain token exists only in the email that
/// was sent.
/// </para>
/// </summary>
public class ExamLink : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AssignmentId { get; set; }
    public Guid ExamId { get; set; }
    public Guid CandidateId { get; set; }

    /// <summary>SHA-256 of the token. Lookups hash the incoming value and compare.</summary>
    public string TokenHash { get; set; } = default!;

    /// <summary>First characters of the token, for support staff to identify a link without holding it.</summary>
    public string TokenPrefix { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }

    public int MaxAttempts { get; set; } = 1;

    /// <summary>Incremented when an attempt actually starts — never on a mere validity check.</summary>
    public int AttemptsUsed { get; set; }

    /// <summary>Revoked links report themselves as revoked rather than as invalid.</summary>
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public DateTime? FirstOpenedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }

    protected ExamLink() { }

    public ExamLink(Guid id, Guid? tenantId, Guid assignmentId, Guid examId, Guid candidateId,
                    string tokenHash, string tokenPrefix, DateTime expiresAt, int maxAttempts) : base(id)
    {
        TenantId = tenantId;
        AssignmentId = assignmentId;
        ExamId = examId;
        CandidateId = candidateId;
        TokenHash = tokenHash;
        TokenPrefix = tokenPrefix;
        ExpiresAt = expiresAt;
        MaxAttempts = maxAttempts;
    }

    /// <summary>
    /// Why this link cannot be used, or null when it can. Returns a specific reason
    /// so the taker is told what went wrong instead of a bare failure.
    /// </summary>
    public string? GetBlockReason(DateTime now)
    {
        if (IsRevoked)
        {
            return InternshipManagementSystemDomainErrorCodes.ExamLinkRevoked;
        }

        if (ExpiresAt < now)
        {
            return InternshipManagementSystemDomainErrorCodes.ExamLinkExpired;
        }

        if (AttemptsUsed >= MaxAttempts)
        {
            return InternshipManagementSystemDomainErrorCodes.ExamLinkAttemptsExhausted;
        }

        return null;
    }
}
