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
    public class ExamController : AbpController, IExamAppService
    {
        private readonly IExamAppService _examAppService;

        public ExamController(IExamAppService examAppService)
        {
            _examAppService = examAppService;
        }

        [HttpGet]
        public Task<PagedResultDto<ExamDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _examAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        public Task<ExamDto> GetAsync(Guid id)
        {
            return _examAppService.GetAsync(id);
        }

        [HttpPost]
        public Task<ExamDto> CreateAsync(CreateUpdateExamDto input)
        {
            return _examAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public Task<ExamDto> UpdateAsync(Guid id, CreateUpdateExamDto input)
        {
            return _examAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public Task DeleteAsync(Guid id)
        {
            return _examAppService.DeleteAsync(id);
        }
    }
}