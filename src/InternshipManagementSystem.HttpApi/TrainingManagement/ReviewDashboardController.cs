using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.TrainingManagement.Grading;
using InternshipManagementSystem.TrainingManagement.Grading.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(InternshipManagementSystemPermissions.TrainingManagement.ManualReviewDashboard.View)]
    [Route("api/review-dashboard")]
    public class ReviewDashboardController : ControllerBase
    {
        private readonly IReviewDashboardAppService _service;

        public ReviewDashboardController(IReviewDashboardAppService service)
        {
            _service = service;
        }

        [HttpGet("manual-review-attempts")]
        public Task<ListResultDto<ManualReviewAttemptDto>> GetAttemptsNeedingManualReviewAsync()
        {
            return _service.GetAttemptsNeedingManualReviewAsync();
        }
    }
}
