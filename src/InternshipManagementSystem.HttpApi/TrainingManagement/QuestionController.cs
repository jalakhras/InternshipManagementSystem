using InternshipManagementSystem.TrainingManagement.DTOs.Questions;
using InternshipManagementSystem.TrainingManagement.Questions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.TrainingManagement
{
    [RemoteService]
    [Route("api/training-management/questions")]
    public class QuestionController : AbpController, IQuestionAppService
    {
        private readonly IQuestionAppService _questionAppService;

        public QuestionController(IQuestionAppService questionAppService)
        {
            _questionAppService = questionAppService;
        }

        [HttpGet]
        public Task<PagedResultDto<QuestionDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _questionAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        public Task<QuestionDto> GetAsync(Guid id)
        {
            return _questionAppService.GetAsync(id);
        }

        [HttpPost]
        public Task<QuestionDto> CreateAsync(CreateUpdateQuestionDto input)
        {
            return _questionAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public Task<QuestionDto> UpdateAsync(Guid id, CreateUpdateQuestionDto input)
        {
            return _questionAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public Task DeleteAsync(Guid id)
        {
            return _questionAppService.DeleteAsync(id);
        }
    }

}
