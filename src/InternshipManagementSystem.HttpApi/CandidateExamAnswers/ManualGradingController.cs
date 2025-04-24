using InternshipManagementSystem.CandidateExamAnswers;
using InternshipManagementSystem.CandidateExamAnswers.DTOs;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.ManualGrading
{
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ManualGrading.Default)]
    [Route("api/manual-grading")]
    public class ManualGradingController : AbpController
    {
        private readonly IManualGradingAppService _manualGradingAppService;

        public ManualGradingController(IManualGradingAppService manualGradingAppService)
        {
            _manualGradingAppService = manualGradingAppService;
        }

        [HttpGet("attempt/{attemptId}/answers")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ManualGrading.View)]
        public Task<ListResultDto<CandidateExamAnswerForReviewDto>> GetAnswersForManualReviewAsync(Guid attemptId)
        {
            return _manualGradingAppService.GetAnswersForManualReviewAsync(attemptId);
        }

        [HttpPut("answer/{answerId}/review")]
        [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ManualGrading.Edit)]
        public Task ReviewAnswerAsync(Guid answerId, [FromBody] ManualGradingInputDto input)
        {
            return _manualGradingAppService.ReviewAnswerAsync(answerId, input);
        }
    }
}
