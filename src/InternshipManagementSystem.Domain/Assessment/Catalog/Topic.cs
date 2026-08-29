using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Catalog;

/// <summary>
/// A competency a question measures. Topics turn a single score into a profile:
/// "SQL 85%, algorithms 40%" tells a hiring manager what to do; "62%" does not.
/// <para>
/// Topics also drive the blueprint (so every generated form covers the same ground)
/// and item analysis (so a weak question can be traced to the competency it claims
/// to measure).
/// </para>
/// </summary>
public class Topic : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>The domain this competency belongs to. Scoped for the same reason as a level.</summary>
    public Guid? CategoryId { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>Stable machine key, unique per tenant.</summary>
    public string Code { get; set; } = default!;

    /// <summary>Optional parent, for tenants that want a two-level competency tree.</summary>
    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    protected Topic() { }

    public Topic(Guid id, Guid? tenantId, string code, string name, Guid? parentId = null) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        ParentId = parentId;
    }
}
