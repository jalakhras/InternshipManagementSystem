using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>Membership of a <see cref="CandidateGroup"/>. A person may belong to several.</summary>
public class CandidateGroupMember : AuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid CandidateGroupId { get; set; }
    public Guid CandidateId { get; set; }

    public CandidateGroup? CandidateGroup { get; set; }
    public Candidate? Candidate { get; set; }

    protected CandidateGroupMember() { }

    public CandidateGroupMember(Guid id, Guid? tenantId, Guid candidateGroupId, Guid candidateId) : base(id)
    {
        TenantId = tenantId;
        CandidateGroupId = candidateGroupId;
        CandidateId = candidateId;
    }
}
