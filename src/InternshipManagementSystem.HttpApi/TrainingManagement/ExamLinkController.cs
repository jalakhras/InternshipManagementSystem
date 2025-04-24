using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.ExamLinks;
using InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts;
using InternshipManagementSystem.TrainingManagement.CandidateExamAttempts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.CandidateExamAttempts;

namespace InternshipManagementSystem.Controllers.TrainingManagement
{
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamLinks.Default)]
    [Route("api/training-management/exam-links")]
    public class ExamLinkController : ControllerBase
    {
        private readonly IExamLinkAppService _examLinkAppService;
        private readonly ICandidateExamAttemptAppService _candidateExamAttemptAppService;

        public ExamLinkController(
            IExamLinkAppService examLinkAppService,
            ICandidateExamAttemptAppService candidateExamAttemptAppService)
        {
            _examLinkAppService = examLinkAppService;
            _candidateExamAttemptAppService = candidateExamAttemptAppService;
        }

        [HttpPost]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamLinks.Create)]
        public Task<ExamLinkDto> CreateAsync(CreateExamLinkInput input)
        {
            return _examLinkAppService.GenerateExamLinkAsync(input);
        }

        [HttpGet("by-candidate/{candidateId}")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ExamLinks.View)]
        public async Task<ListResultDto<ExamLinkDto>> GetLinksByCandidateAsync(Guid candidateId)
        {
            var list = await _examLinkAppService.GetLinksForCandidateAsync(candidateId);
            return new ListResultDto<ExamLinkDto>(list);
        }

        [HttpGet("validate/{token}")]
        [AllowAnonymous]
        public async Task<ExamLinkValidationResultDto> ValidateExamLinkAsync(string token)
        {
            var isValid = await _examLinkAppService.ValidateExamLinkAsync(token);
            return new ExamLinkValidationResultDto
            {
                IsValid = isValid,
                Message = isValid ? "Valid link." : "Invalid or expired link."
            };
        }

        [HttpGet("access/{token}")]
        [AllowAnonymous]
        public async Task<ExamAccessResultDto> AccessExamByTokenAsync(string token)
        {
            return await _examLinkAppService.GetExamAccessDetailsByTokenAsync(token);
        }

        [HttpPost("start/{token}")]
        [AllowAnonymous]
        public async Task<StartExamFromLinkResultDto> StartExamByTokenAsync(string token)
        {
            var result = await _examLinkAppService.StartExamByTokenAsync(token);
            if (result.IsStarted)
            {
                var attemptDto = new CreateCandidateExamAttemptDto
                {
                    CandidateId = result.CandidateId.Value,
                    ExamId = result.ExamId.Value
                };

                await _candidateExamAttemptAppService.CreateCandidateExamAttemptAsync(attemptDto);
            }

            return result;
        }
    }
}
