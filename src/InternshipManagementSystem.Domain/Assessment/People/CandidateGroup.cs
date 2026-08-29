using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// A set of people assessed together: "Spanish B1 — Autumn 2026", "March hiring batch".
/// <para>
/// Without this, sending an exam to forty students means creating forty links by
/// hand. That is the difference between a system a training centre uses and one it
/// opens once and abandons for a spreadsheet.
/// </para>
/// </summary>
public class CandidateGroup : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>Which category this cohort belongs to, when it maps to one.</summary>
    public Guid? CategoryId { get; set; }

    public Guid? LevelId { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CandidateGroupMember> Members { get; set; } = new List<CandidateGroupMember>();

    protected CandidateGroup() { }

    public CandidateGroup(Guid id, Guid? tenantId, string name) : base(id)
    {
        TenantId = tenantId;
        Name = name;
    }
}
