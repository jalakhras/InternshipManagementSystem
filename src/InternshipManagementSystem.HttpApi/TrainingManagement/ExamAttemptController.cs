using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts;
using InternshipManagementSystem.TrainingManagement.ExamAttempts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.TrainingManagement
{
    [RemoteService]
    [Route("api/training-management/exam-attempts")]
    public class ExamAttemptController : AbpController, IExamAttemptAppService
    {
        private readonly IExamAttemptAppService _examAttemptAppService;

        public ExamAttemptController(IExamAttemptAppService examAttemptAppService)
        {
            _examAttemptAppService = examAttemptAppService;
        }

        [HttpGet]
        [Route("{id}")]
        public virtual Task<ExamAttemptDto> GetAsync(Guid id)
        {
            return _examAttemptAppService.GetAsync(id);
        }

        [HttpGet]
        [Route("by-trainee/{traineeId}")]
        public virtual Task<List<ExamAttemptDto>> GetAttemptsByTraineeIdAsync(Guid traineeId)
        {
            return _examAttemptAppService.GetAttemptsByTraineeIdAsync(traineeId);
        }

        [HttpGet]
        [Route("by-exam/{examId}")]
        public virtual Task<List<ExamAttemptDto>> GetAttemptsByExamIdAsync(Guid examId)
        {
            return _examAttemptAppService.GetAttemptsByExamIdAsync(examId);
        }

        [HttpPost]
        public virtual Task<ExamAttemptDto> CreateAsync(CreateUpdateExamAttemptDto input)
        {
            return _examAttemptAppService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        public virtual Task<ExamAttemptDto> UpdateAsync(Guid id, CreateUpdateExamAttemptDto input)
        {
            return _examAttemptAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return _examAttemptAppService.DeleteAsync(id);
        }

        [HttpGet]
        public async Task<PagedResultDto<ExamAttemptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return await _examAttemptAppService.GetListAsync(input);
        }

    }
}
