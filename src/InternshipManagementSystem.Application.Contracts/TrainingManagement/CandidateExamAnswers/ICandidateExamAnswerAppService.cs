using InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.CandidateExamAnswers
{
    public interface ICandidateExamAnswerAppService :
        ICrudAppService<
            CandidateExamAnswerDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateCandidateExamAnswerDto>
    {
    }
}