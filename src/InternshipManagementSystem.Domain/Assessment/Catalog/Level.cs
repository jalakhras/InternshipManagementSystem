using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Catalog;

/// <summary>
/// The second axis: how advanced the exam is within its <see cref="Category"/>.
/// "B1" for a language school, "Senior" for a recruiter, "Advanced" for a trading
/// academy. Kept separate from Category so "Spanish + B1" is expressible without
/// creating a value per combination.
/// </summary>
public class Level : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>Stable machine key, unique per tenant.</summary>
    public string Code { get; set; } = default!;

    /// <summary>Sort order, lowest first. Carries the ranking a level name implies.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    protected Level() { }

    public Level(Guid id, Guid? tenantId, string code, string name, int displayOrder = 0) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        DisplayOrder = displayOrder;
    }
}
