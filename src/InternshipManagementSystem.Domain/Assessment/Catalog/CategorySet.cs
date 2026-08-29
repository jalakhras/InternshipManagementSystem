using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Catalog;

/// <summary>
/// What this tenant calls its category axis, so the UI can label it in the
/// tenant's own words instead of showing everyone the word "Category".
/// One row per tenant.
/// </summary>
public class CategorySet : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>Singular label: "Job Role", "Language", "Track".</summary>
    public string SingularName { get; set; } = default!;

    /// <summary>Plural label: "Job Roles", "Languages", "Tracks".</summary>
    public string PluralName { get; set; } = default!;

    /// <summary>What this tenant calls the people it assesses: "Candidate", "Student", "Trainee".</summary>
    public string SubjectSingularName { get; set; } = "Candidate";

    public string SubjectPluralName { get; set; } = "Candidates";

    /// <summary>What this tenant calls a cohort: "Batch", "Class", "Cohort".</summary>
    public string GroupSingularName { get; set; } = "Group";

    public string GroupPluralName { get; set; } = "Groups";

    protected CategorySet() { }

    public CategorySet(Guid id, Guid? tenantId, string singularName, string pluralName) : base(id)
    {
        TenantId = tenantId;
        SingularName = singularName;
        PluralName = pluralName;
    }
}
