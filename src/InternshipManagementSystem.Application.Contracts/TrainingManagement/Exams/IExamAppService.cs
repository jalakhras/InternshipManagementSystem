using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.Exams
{
    public interface IExamAppService :
    ICrudAppService<
        ExamDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateExamDto>
    {
    }

}