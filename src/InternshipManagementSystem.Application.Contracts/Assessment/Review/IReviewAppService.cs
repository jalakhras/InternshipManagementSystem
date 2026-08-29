using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Review.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Review;

/// <summary>Marking what a machine cannot mark.</summary>
public interface IReviewAppService : IApplicationService
{
    /// <summary>Attempts waiting on a human, oldest first.</summary>
    Task<PagedResultDto<ReviewQueueItemDto>> GetQueueAsync(PagedAndSortedResultRequestDto input);

    /// <summary>The pending answers on one attempt, with rubric, key and context.</summary>
    Task<List<ReviewAnswerDto>> GetAnswersAsync(Guid attemptId);

    /// <summary>
    /// Records a mark and retotals the attempt immediately, closing it out when this
    /// was the last pending answer.
    /// </summary>
    Task GradeAnswerAsync(GradeAnswerDto input);

    /// <summary>Behavioural observations from the attempt. Advisory, for a person to weigh.</summary>
    Task<IntegrityReportDto> GetIntegrityReportAsync(Guid attemptId);
}
