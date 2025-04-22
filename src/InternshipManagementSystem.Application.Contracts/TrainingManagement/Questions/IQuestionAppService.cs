using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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