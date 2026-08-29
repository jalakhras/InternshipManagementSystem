using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// Sending an exam to people, and the links that result.
/// <para>
/// The service existed with no controller, so none of this was reachable over
/// HTTP — the last stretch between a roll of candidates and somebody sitting an
/// exam was written and unwired.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/assignments")]
public class AssignmentController : AbpControllerBase
{
    private readonly IAssignmentAppService _assignments;

    public AssignmentController(IAssignmentAppService assignments)
    {
        _assignments = assignments;
    }

    /// <summary>
    /// Sends an exam to one person or to a whole cohort. Creating the links never
    /// fails because an email did; the result reports both separately.
    /// </summary>
    [HttpPost]
    public Task<AssignmentResultDto> CreateAsync([FromBody] CreateAssignmentDto input) =>
        _assignments.CreateAsync(input);

    [HttpGet("links/{examId}")]
    public Task<PagedResultDto<ExamLinkDto>> GetLinksAsync(
        Guid examId,
        [FromQuery] PagedAndSortedResultRequestDto input) =>
        _assignments.GetLinksAsync(examId, input);

    /// <summary>
    /// A fresh link for the same person. The old one stops working.
    /// </summary>
    [HttpPost("links/{linkId}/reissue")]
    public Task<AssignmentRecipientDto> ReissueLinkAsync(Guid linkId) =>
        _assignments.ReissueLinkAsync(linkId);

    /// <summary>
    /// Moves the deadline forward, for somebody who missed it. Reissuing does
    /// not touch the deadline, so an expired link came back expired.
    /// </summary>
    [HttpPost("links/{linkId}/extend")]
    public Task<ExamLinkDto> ExtendLinkAsync(Guid linkId, [FromBody] ExtendLinkDto input) =>
        _assignments.ExtendLinkAsync(linkId, input.ExpiresAt);

    /// <summary>Kills a link that leaked or went to the wrong person.</summary>
    [HttpPost("links/{linkId}/revoke")]
    public Task RevokeLinkAsync(Guid linkId) => _assignments.RevokeLinkAsync(linkId);
}
