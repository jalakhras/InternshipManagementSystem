using InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.ExamLinks
{
    public interface IExamLinkAppService : IApplicationService
    {
        Task<ExamLinkDto> GenerateExamLinkAsync(CreateExamLinkInput input);

        Task<List<ExamLinkDto>> GetLinksForCandidateAsync(Guid candidateId);

        Task<bool> ValidateExamLinkAsync(string token);
        Task<ExamAccessResultDto> GetExamAccessDetailsByTokenAsync(string token);
        Task<StartExamFromLinkResultDto> StartExamByTokenAsync(string token);

    }
}