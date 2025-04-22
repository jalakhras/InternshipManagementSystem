using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts.DTOs;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.CandidateExamAttempts
{
    public interface ICandidateExamAttemptAppService :
        ICrudAppService<
            CandidateExamAttemptDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateCandidateExamAttemptDto,
            CreateUpdateCandidateExamAttemptDto>
    {
    }
}
