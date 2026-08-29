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

    /// <summary>
    /// When this class begins, and when it ends. Both optional.
    /// <para>
    /// A class is a cohort in time as much as a list of people — "Evening A1,
    /// autumn" is a different class from "Evening A1, spring" with a different
    /// roll and different results, and without dates the two are one row that
    /// keeps being edited.
    /// </para>
    /// </summary>
    public DateTime? StartsOn { get; set; }

    public DateTime? EndsOn { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The papers this class sits, in the order it sits them.
    /// <para>
    /// This is what makes the retake guarantee real. A form exists so that
    /// sitting an exam again means a genuinely different paper rather than a
    /// redraw that might repeat half the questions — and that only holds if
    /// somebody decided in advance which paper the second sitting uses.
    /// </para>
    /// </summary>
    public ICollection<CandidateGroupForm> Forms { get; set; } = new List<CandidateGroupForm>();

    public ICollection<CandidateGroupMember> Members { get; set; } = new List<CandidateGroupMember>();

    protected CandidateGroup() { }

    public CandidateGroup(Guid id, Guid? tenantId, string name) : base(id)
    {
        TenantId = tenantId;
        Name = name;
    }
}
