using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Results;
using InternshipManagementSystem.Assessment.Results.Dtos;
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
    [HttpGet("export")]
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
