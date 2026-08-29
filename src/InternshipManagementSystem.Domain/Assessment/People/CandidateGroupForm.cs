using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// One paper a class sits, and where it comes in the order.
/// <para>
/// A class sits Form 1 first. Whoever fails sits Form 2. That is the whole idea,
/// and it is the thing that turns the retake guarantee from a property of the
/// schema into a decision somebody actually made.
/// </para>
/// <para>
/// Without this the second sitting is a fresh draw from the same bank, which
/// might repeat half the questions — and a candidate who failed and then passed
/// on largely the same paper has been measured on their memory of the first
/// attempt rather than on what they know.
/// </para>
/// </summary>
public class CandidateGroupForm : AuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid CandidateGroupId { get; set; }

    public Guid ExamFormId { get; set; }

    /// <summary>
    /// Zero for the first sitting, one for the retake, and so on.
    /// <para>
    /// An order rather than a label, because what a tenant calls the second
    /// sitting differs — a resit, a make-up, a second attempt — and the platform
    /// does not need to have an opinion about the word.
    /// </para>
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// When this class sits this paper, if it is scheduled. Optional, because a
    /// self-paced academy has no sitting date and a language centre does.
    /// </summary>
    public DateTime? SittingOn { get; set; }

    protected CandidateGroupForm() { }

    public CandidateGroupForm(Guid id, Guid? tenantId, Guid candidateGroupId, Guid examFormId, int sequence)
        : base(id)
    {
        TenantId = tenantId;
        CandidateGroupId = candidateGroupId;
        ExamFormId = examFormId;
        Sequence = sequence;
    }
}
