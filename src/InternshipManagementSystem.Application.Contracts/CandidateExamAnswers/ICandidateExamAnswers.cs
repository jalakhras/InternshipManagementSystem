using InternshipManagementSystem.CandidateExamAnswers.DTOs;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.CandidateExamAnswers
{
    public interface IManualGradingAppService
    {
        Task<ListResultDto<CandidateExamAnswerForReviewDto>> GetAnswersForManualReviewAsync(Guid attemptId);
        Task ReviewAnswerAsync(Guid answerId, ManualGradingInputDto input);
    }
}
