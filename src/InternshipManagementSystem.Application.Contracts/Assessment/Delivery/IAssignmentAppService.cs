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
    Task RevokeLinkAsync(Guid linkId);
}
