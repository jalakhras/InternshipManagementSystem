using InternshipManagementSystem.CandidateExamAttempts.DTOs;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.ExamResults;
using InternshipManagementSystem.TrainingManagement.ExamResults.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Controllers.TrainingManagement
{
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.Exams.View)]
    [Route("api/training-management/exam-results-dashboard")]
    public class ExamResultsDashboardController : ControllerBase
    {
        private readonly IExamResultsDashboardAppService _dashboardService;

        public ExamResultsDashboardController(IExamResultsDashboardAppService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public Task<ListResultDto<ExamResultSummaryDto>> GetExamResultsOverviewAsync()
        {
            return _dashboardService.GetExamResultsOverviewAsync();
        }

        [HttpGet("by-exam/{examId}")]
        public Task<ListResultDto<CandidateExamAttemptResultDto>> GetResultsByExamAsync(Guid examId)
        {
            return _dashboardService.GetResultsByExamAsync(examId);
        }

        [HttpGet("by-candidate/{candidateId}")]
        public Task<ListResultDto<CandidateExamAttemptResultDto>> GetResultsByCandidateAsync(Guid candidateId)
        {
            return _dashboardService.GetResultsByCandidateAsync(candidateId);
        }
    }
}