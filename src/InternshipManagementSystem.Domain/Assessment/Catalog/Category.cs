using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Catalog;

/// <summary>
/// The axis a tenant files its exams under. The platform does not know what that
/// axis means — a recruiter calls it "Job Role", a language school calls it
/// "Language", a trading academy calls it "Track". The tenant names both the axis
/// (<see cref="CategorySet"/>) and its values.
/// </summary>
public class Category : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>Display name of this value: "Backend Developer", "Spanish", "Risk Management".</summary>
    public string Name { get; set; } = default!;

    /// <summary>Stable machine key, unique per tenant. Used in imports and reporting.</summary>
    public string Code { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>Sort order within the tenant's list.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    protected Category() { }

    public Category(Guid id, Guid? tenantId, string code, string name) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
    }
}
