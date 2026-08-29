using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>Handing exams out, and controlling the links that result.</summary>
public interface IAssignmentAppService : IApplicationService
{
    /// <summary>
    /// Assigns an exam to one person or a whole cohort, minting a distinct link for
    /// each recipient and emailing them. Returns the URLs once, so an operator can
    /// recover from a bounced email; they are not retrievable afterwards.
    /// </summary>
    Task<AssignmentResultDto> CreateAsync(CreateAssignmentDto input);

    /// <summary>Links issued for an exam, with delivery and usage state.</summary>
    Task<PagedResultDto<ExamLinkDto>> GetLinksAsync(Guid examId, PagedAndSortedResultRequestDto input);

    /// <summary>Stops a link working, for a leak or a mistaken send.</summary>
    /// <summary>
    /// Moves a link's deadline forward, for somebody who missed it.
    /// <para>
    /// Separate from reissuing: a lost address and a missed deadline are
    /// different problems, and reissuing an expired link produced a new token
    /// that was expired the moment it was handed over.
    /// </para>
    /// </summary>
    Task<ExamLinkDto> ExtendLinkAsync(Guid linkId, DateTime expiresAt);

    Task RevokeLinkAsync(Guid linkId);

    /// <summary>
    /// Issues a fresh link for the same person and the same sitting.
    /// <para>
    /// A token is stored hashed and cannot be recovered — only its first few
    /// characters survive, which is enough to tell two apart and not enough to
    /// use. So the panel that appears after sending is the only place the link
    /// can be copied, and a coordinator who closes it has lost it.
    /// </para>
    /// <para>
    /// This is the honest answer to that: not to keep the credential lying about
    /// in readable form, but to be able to issue another. The old link stops
    /// working the moment this one is made, because two live links for one
    /// sitting is two ways to spend an attempt.
    /// </para>
    /// <para>
    /// Attempts already used are carried over. Reissuing a link is not a way to
    /// give somebody another go at an exam they have already sat.
    /// </para>
    /// </summary>
    Task<AssignmentRecipientDto> ReissueLinkAsync(Guid linkId);
}
