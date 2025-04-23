using InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.ExamAttempts
{
    public interface IExamAttemptAppService :
        ICrudAppService< // هذا يوفر (Get, GetList, Create, Update, Delete)
            ExamAttemptDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateExamAttemptDto>
    {
        Task<List<ExamAttemptDto>> GetAttemptsByTraineeIdAsync(Guid traineeId);

        Task<List<ExamAttemptDto>> GetAttemptsByExamIdAsync(Guid examId);

        Task SubmitAttemptAsync(Guid attemptId);
    }
}