using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// A person being assessed. Deliberately not an <c>IdentityUser</c>: they reach an
/// exam through a signed link, not a login, which removes the sign-up step and stops
/// the tenant accumulating dormant accounts.
/// <para>
/// The tenant decides what this person is called in its own UI — candidate, student,
/// trainee. See <c>CategorySet</c>.
/// </para>
/// <para>
/// This replaces the old parallel <c>Candidate</c> / <c>Trainee</c> pair, which
/// carried two entity chains and two grading services for the same concept.
/// </para>
/// </summary>
public class Candidate : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? PhoneNumber { get; set; }

    /// <summary>What they applied for, are enrolled in, or are being assessed against.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Free-text detail the tenant wants to keep: a job title, a course name.</summary>
    public string? Reference { get; set; }

    public CandidateStatus Status { get; set; } = CandidateStatus.Pending;

    /// <summary>Set when the tenant also gave this person a login. Usually null.</summary>
    public Guid? UserId { get; set; }

    public ICollection<CandidateGroupMember> GroupMemberships { get; set; } = new List<CandidateGroupMember>();

    protected Candidate() { }

    public Candidate(Guid id, Guid? tenantId, string fullName, string email) : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
    }
}
