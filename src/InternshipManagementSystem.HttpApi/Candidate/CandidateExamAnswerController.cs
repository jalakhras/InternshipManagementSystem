using Asp.Versioning;
using InternshipManagementSystem.TrainingManagement.CandidateExamAnswers;
using InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Candidate
{
    [RemoteService]
    [Area("app")]
    [ControllerName("CandidateExamAnswer")]
    [Route("api/app/candidate-exam-answers")]
    [Authorize]
    public class CandidateExamAnswerController : AbpController, ICandidateExamAnswerAppService
    {
        private readonly ICandidateExamAnswerAppService _service;

        public CandidateExamAnswerController(ICandidateExamAnswerAppService service)
        {
            _service = service;
        }

        [HttpPost]
        public virtual Task<CandidateExamAnswerDto> CreateAsync([FromForm] CreateUpdateCandidateExamAnswerDto input)
        {
            return _service.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public virtual Task<CandidateExamAnswerDto> UpdateAsync(Guid id, [FromForm] CreateUpdateCandidateExamAnswerDto input)
        {
            return _service.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return _service.DeleteAsync(id);
        }

        [HttpGet("{id}")]
        public virtual Task<CandidateExamAnswerDto> GetAsync(Guid id)
        {
            return _service.GetAsync(id);
        }

        [HttpGet]
        public virtual Task<PagedResultDto<CandidateExamAnswerDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _service.GetListAsync(input);
        }
    }
}
