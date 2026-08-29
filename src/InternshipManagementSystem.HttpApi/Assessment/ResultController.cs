using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Results;
using InternshipManagementSystem.Assessment.Results.Dtos;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// What happened when people sat the exam.
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/results")]
public class ResultController : AbpControllerBase
{
    private readonly IResultAppService _results;

    public ResultController(IResultAppService results)
    {
        _results = results;
    }

    [HttpGet]
    public Task<PagedResultDto<ResultRowDto>> GetListAsync([FromQuery] ResultListRequestDto input) =>
        _results.GetListAsync(input);

    /// <summary>Declared before {attemptId} so "summary" is not read as an id.</summary>
    [HttpGet("summary")]
    public Task<ResultSummaryDto> GetSummaryAsync([FromQuery] ResultListRequestDto input) =>
        _results.GetSummaryAsync(input);

    /// <summary>
    /// The roster as a file.
    /// <para>
    /// Returned as a download rather than a JSON string, because the thing a
    /// coordinator wants is a file in their downloads folder, and asking the
    /// browser to build one out of an API response is asking the front end to
    /// reimplement the browser.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The two permissions are repeated here, on top of the ones the app service
    /// already carries, and this is the only action in the file that needs it.
    /// ABP's exception filter turns an <c>AbpAuthorizationException</c> into a 403
    /// only for actions that return an object result; this one returns
    /// <c>IActionResult</c>, so the refusal escaped the filter and came back as a
    /// 500. A person without the permission was told the product was broken
    /// rather than that they were not allowed, and whoever they reported it to
    /// would have gone looking for a bug in the export. Declared as attributes so
    /// ASP.NET refuses before the action runs and no exception is thrown at all.
    /// They combine with AND, which matches the service exactly: the roster is
    /// behind View, and taking a copy of it away is behind Export.
    /// </remarks>
    [HttpGet("export")]
    [Authorize(InternshipManagementSystemPermissions.Results.View)]
    [Authorize(InternshipManagementSystemPermissions.Results.Export)]
    public async Task<IActionResult> ExportAsync([FromQuery] ResultListRequestDto input)
    {
        var csv = await _results.ExportCsvAsync(input);
        var name = $"results-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", name);
    }

    [HttpGet("item-analysis/{examId}")]
    public Task<List<ItemAnalysisRowDto>> GetItemAnalysisAsync(Guid examId) =>
        _results.GetItemAnalysisAsync(examId);

    [HttpGet("{attemptId}")]
    public Task<ResultDetailDto> GetAsync(Guid attemptId) => _results.GetAsync(attemptId);
}
