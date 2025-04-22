using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using InternshipManagementSystem.TrainingManagement.ExamAnswers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.TrainingManagement
{
    [RemoteService]
    [Route("api/training-management/exam-answers")]
    public class ExamAnswerController : AbpController, IExamAnswerAppService
    {
        private readonly IExamAnswerAppService _examAnswerAppService;

        public ExamAnswerController(IExamAnswerAppService examAnswerAppService)
        {
            _examAnswerAppService = examAnswerAppService;
        }

        [HttpGet]
        public Task<PagedResultDto<ExamAnswerDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _examAnswerAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        public Task<ExamAnswerDto> GetAsync(Guid id)
        {
            return _examAnswerAppService.GetAsync(id);
        }

        [HttpPost]
        public Task<ExamAnswerDto> CreateAsync(CreateUpdateExamAnswerDto input)
        {
            return _examAnswerAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public Task<ExamAnswerDto> UpdateAsync(Guid id, CreateUpdateExamAnswerDto input)
        {
            return _examAnswerAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public Task DeleteAsync(Guid id)
        {
            return _examAnswerAppService.DeleteAsync(id);
        }
    }

}