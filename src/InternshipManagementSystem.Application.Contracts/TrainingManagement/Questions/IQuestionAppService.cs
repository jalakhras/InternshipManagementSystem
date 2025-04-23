using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.Questions
{
    public interface IQuestionAppService :
     ICrudAppService<
         QuestionDto,
         Guid,
         PagedAndSortedResultRequestDto,
         CreateUpdateQuestionDto>
    {
    }
}