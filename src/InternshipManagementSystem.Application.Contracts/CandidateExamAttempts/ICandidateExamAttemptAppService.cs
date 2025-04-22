using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using InternshipManagementSystem.CandidateExamAttempts.DTOs;

namespace InternshipManagementSystem.CandidateExamAttempts
{
    public interface ICandidateExamAttemptAppService : IApplicationService
    {
        Task<CandidateExamAttemptDto> CreateCandidateExamAttemptAsync(CreateCandidateExamAttemptDto input);
        Task SubmitCandidateExamAttemptAsync(SubmitCandidateExamAttemptDto input);
        Task<CandidateExamAttemptDto> GetAttemptForCandidateAsync(Guid candidateId, Guid examId);
        Task<CandidateExamAttemptResultDto> GetAttemptResultAsync(Guid attemptId);
    }
}
