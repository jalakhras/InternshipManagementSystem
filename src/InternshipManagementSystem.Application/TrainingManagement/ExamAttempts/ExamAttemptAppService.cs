using InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.TrainingManagement.ExamAttempts
{
    public class ExamAttemptAppService :
        CrudAppService<
            ExamAttempt,
            ExamAttemptDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateExamAttemptDto>,
        IExamAttemptAppService
    {
        public ExamAttemptAppService(IRepository<ExamAttempt, Guid> repository)
            : base(repository)
        {
        }

        public async Task<List<ExamAttemptDto>> GetAttemptsByTraineeIdAsync(Guid traineeId)
        {
            var queryable = await Repository.GetQueryableAsync();
            var attempts =  queryable
                .Where(x => x.TraineeId == traineeId)
                .ToList();

            return ObjectMapper.Map<List<ExamAttempt>, List<ExamAttemptDto>>(attempts);
        }

        public async Task<List<ExamAttemptDto>> GetAttemptsByExamIdAsync(Guid examId)
        {
            var queryable = await Repository.GetQueryableAsync();
            var attempts =  queryable
                .Where(x => x.ExamId == examId)
                .ToList();

            return ObjectMapper.Map<List<ExamAttempt>, List<ExamAttemptDto>>(attempts);
        }

    }
}