using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.ExamAnswers
{
    public interface IExamAnswerAppService :
        ICrudAppService<
            ExamAnswerDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateExamAnswerDto>
    {
    }
}