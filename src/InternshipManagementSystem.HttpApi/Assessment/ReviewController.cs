using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Review;
using InternshipManagementSystem.Assessment.Review.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// The queue of attempts waiting on a person.
/// <para>
/// The service was written and had no controller, so a reviewer had a screen to
/// build against nothing. Third time this pattern has appeared in this codebase:
/// a finished application service with no HTTP surface reads as done in every
/// listing that counts services rather than routes.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/review")]
public class ReviewController : AbpControllerBase
{
    private readonly IReviewAppService _review;

    public ReviewController(IReviewAppService review)
    {
        _review = review;
    }

    [HttpGet("queue")]
    public Task<PagedResultDto<ReviewQueueItemDto>> GetQueueAsync(
        [FromQuery] PagedAndSortedResultRequestDto input) =>
        _review.GetQueueAsync(input);

    /// <summary>
    /// The answers on one attempt that need marking, with their rubrics.
    /// <para>
    /// Answer-bearing: it carries the reviewer's guidance and the rendered key.
    /// </para>
    /// </summary>
    [HttpGet("attempts/{attemptId}")]
    public Task<List<ReviewAnswerDto>> GetAnswersAsync(Guid attemptId) =>
        _review.GetAnswersAsync(attemptId);

    /// <summary>Records a mark and retotals the attempt immediately.</summary>
    [HttpPost("grade")]
    public Task GradeAnswerAsync([FromBody] GradeAnswerDto input) => _review.GradeAnswerAsync(input);

    /// <summary>Behavioural observations from the attempt. Advisory, for a person to weigh.</summary>
    [HttpGet("attempts/{attemptId}/integrity")]
    public Task<IntegrityReportDto> GetIntegrityReportAsync(Guid attemptId) =>
        _review.GetIntegrityReportAsync(attemptId);
}
