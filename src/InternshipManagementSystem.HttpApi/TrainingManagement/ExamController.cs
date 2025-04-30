using InternshipManagementSystem.TrainingManagement.DTOs.Exams;
using InternshipManagementSystem.TrainingManagement.Exams;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.TrainingManagement
{
    [RemoteService]
    [Route("api/training-management/exams")]
    public class ExamController : AbpController
    {
        private readonly IExamAppService _examAppService;

        public ExamController(IExamAppService examAppService)
        {
            _examAppService = examAppService;
        }

        [HttpGet]
        public async Task<PagedResultDto<ExamDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return await _examAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        public async Task<ExamDto> GetAsync(Guid id)
        {
            return await _examAppService.GetAsync(id);
        }

        [HttpPost]
        public async Task<ExamDto> CreateAsync(CreateUpdateExamDto input)
        {
            return await _examAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public async Task<ExamDto> UpdateAsync(Guid id, CreateUpdateExamDto input)
        {
            return await _examAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(Guid id)
        {
              await _examAppService.DeleteAsync(id);
        }
    }
}