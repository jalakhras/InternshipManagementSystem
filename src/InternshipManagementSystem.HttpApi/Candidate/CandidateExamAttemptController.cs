using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.CandidateExamAttempts
{
    [Route("api/candidate-exam-attempts")]
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default)]
    public class CandidateExamAttemptController : AbpController, ICandidateExamAttemptAppService
    {
        private readonly ICandidateExamAttemptAppService _candidateExamAttemptAppService;

        public CandidateExamAttemptController(ICandidateExamAttemptAppService candidateExamAttemptAppService)
        {
            _candidateExamAttemptAppService = candidateExamAttemptAppService;
        }

        [HttpPost]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Create)]
        public Task<CandidateExamAttemptDto> CreateAsync(CreateUpdateCandidateExamAttemptDto input)
        {
            return _candidateExamAttemptAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Edit)]
        public Task<CandidateExamAttemptDto> UpdateAsync(Guid id, CreateUpdateCandidateExamAttemptDto input)
        {
            return _candidateExamAttemptAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Delete)]
        public Task DeleteAsync(Guid id)
        {
            return _candidateExamAttemptAppService.DeleteAsync(id);
        }

        [HttpGet("{id}")]
        public Task<CandidateExamAttemptDto> GetAsync(Guid id)
        {
            return _candidateExamAttemptAppService.GetAsync(id);
        }

        [HttpGet]
        public Task<PagedResultDto<CandidateExamAttemptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _candidateExamAttemptAppService.GetListAsync(input);
        }
    }
}