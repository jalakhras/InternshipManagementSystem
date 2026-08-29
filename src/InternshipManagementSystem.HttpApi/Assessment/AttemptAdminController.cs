using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Results.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// Sittings in progress, and what a coordinator can do about one.
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/attempts")]
public class AttemptAdminController : AbpControllerBase
{
    private readonly IAttemptAdminAppService _attempts;

    public AttemptAdminController(IAttemptAdminAppService attempts)
    {
        _attempts = attempts;
    }

    [HttpGet("running")]
    public Task<PagedResultDto<ResultRowDto>> GetRunningAsync(
        [FromQuery] RunningAttemptRequestDto input) => _attempts.GetRunningAsync(input);

    [HttpPost("{attemptId}/end")]
    public Task<ResultRowDto> ForceSubmitAsync(Guid attemptId, [FromBody] ForceSubmitDto input) =>
        _attempts.ForceSubmitAsync(attemptId, input);

    [HttpDelete("{attemptId}")]
    public Task DeleteAsync(Guid attemptId) => _attempts.DeleteAsync(attemptId);
}
