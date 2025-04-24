using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.TrainingManagement.ExamResults.DTOs;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.ExamResults
{
    public interface IExamResultsDashboardAppService : IApplicationService
    {
        Task<ListResultDto<ExamResultSummaryDto>> GetExamResultsOverviewAsync();

        Task<ListResultDto<CandidateExamAttemptResultDto>> GetResultsByExamAsync(Guid examId);

        Task<ListResultDto<CandidateExamAttemptResultDto>> GetResultsByCandidateAsync(Guid candidateId);
    }
}