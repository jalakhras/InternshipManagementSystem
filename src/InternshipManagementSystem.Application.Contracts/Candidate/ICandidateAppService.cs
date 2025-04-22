using InternshipManagementSystem.TrainingManagement.Candidates.DTOs;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.Candidates
{
    public interface ICandidateAppService :
        ICrudAppService<
            CandidateDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateCandidateDto,
            CreateUpdateCandidateDto>
    {
    }
}
