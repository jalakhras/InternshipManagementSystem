using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.Candidates;
using InternshipManagementSystem.TrainingManagement.Candidates.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Candidates
{
    [Route("api/candidates")]
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Default)]
    public class CandidateController : AbpController, ICandidateAppService
    {
        private readonly ICandidateAppService _candidateAppService;

        public CandidateController(ICandidateAppService candidateAppService)
        {
            _candidateAppService = candidateAppService;
        }

        [HttpGet]
        public Task<PagedResultDto<CandidateDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _candidateAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        public Task<CandidateDto> GetAsync(Guid id)
        {
            return _candidateAppService.GetAsync(id);
        }

        [HttpPost]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Create)]
        public Task<CandidateDto> CreateAsync(CreateUpdateCandidateDto input)
        {
            return _candidateAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Edit)]
        public Task<CandidateDto> UpdateAsync(Guid id, CreateUpdateCandidateDto input)
        {
            return _candidateAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.Candidates.Delete)]
        public Task DeleteAsync(Guid id)
        {
            return _candidateAppService.DeleteAsync(id);
        }
    }
}