using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.Controllers;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.CandidateExamAttempts
{
    [Route("api/candidate-exam-attempts")]
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.Default)]
    public class CandidateExamAttemptController : AbpController, ICandidateExamAttemptAppService
    {
        private readonly ICandidateExamAttemptAppService _service;

        public CandidateExamAttemptController(ICandidateExamAttemptAppService service)
        {
            _service = service;
        }

        [HttpPost]
        [Route("create")]
        public Task<CandidateExamAttemptDto> CreateCandidateExamAttemptAsync(CreateCandidateExamAttemptDto input)
        {
            return _service.CreateCandidateExamAttemptAsync(input);
        }

        [HttpPost]
        [Route("submit")]
        public Task SubmitCandidateExamAttemptAsync(SubmitCandidateExamAttemptDto input)
        {
            return _service.SubmitCandidateExamAttemptAsync(input);
        }

        [HttpGet]
        [Route("{candidateId}/exam/{examId}")]
        public Task<CandidateExamAttemptDto> GetAttemptForCandidateAsync(Guid candidateId, Guid examId)
        {
            return _service.GetAttemptForCandidateAsync(candidateId, examId);
        }

        [HttpGet]
        [Route("{attemptId}/result")]
        public Task<CandidateExamAttemptResultDto> GetAttemptResultAsync(Guid attemptId)
        {
            return _service.GetAttemptResultAsync(attemptId);
        } 
        
        [HttpGet]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.CandidateExamAttempts.View)]
        public Task<PagedResultDto<CandidateExamAttemptDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _service.GetListAsync(input);
        }

       
    }
}