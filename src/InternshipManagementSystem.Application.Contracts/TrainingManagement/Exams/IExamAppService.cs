using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using System;
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