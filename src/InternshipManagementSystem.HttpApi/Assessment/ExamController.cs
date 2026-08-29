using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// Exam authoring.
/// <para>
/// Routes are declared explicitly rather than left to ABP's conventional API
/// generation. The Angular client is hand-written — ABP's proxy schematic does not
/// run against Angular 22 — so the URLs are part of the contract and should be
/// visible in one place rather than inferred from a naming convention.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/exams")]
public class ExamController : AbpControllerBase
{
    private readonly IExamAppService _exams;

    public ExamController(IExamAppService exams)
    {
        _exams = exams;
    }

    [HttpGet]
    public Task<PagedResultDto<ExamDto>> GetListAsync([FromQuery] ExamListRequestDto input) =>
        _exams.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<ExamDto> GetAsync(Guid id) => _exams.GetAsync(id);

    [HttpPost]
    public Task<ExamDto> CreateAsync([FromBody] CreateUpdateExamDto input) => _exams.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<ExamDto> UpdateAsync(Guid id, [FromBody] CreateUpdateExamDto input) =>
        _exams.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => _exams.DeleteAsync(id);

    /// <summary>
    /// What publishing would do. Called before offering the action so the author
    /// sees every blocker at once rather than one refused click at a time.
    /// </summary>
    [HttpGet("{id}/publish-check")]
    public Task<PublishCheckDto> CheckPublishAsync(Guid id) => _exams.CheckPublishAsync(id);

    [HttpPost("{id}/publish")]
    public Task<ExamDto> PublishAsync(Guid id) => _exams.PublishAsync(id);

    [HttpPost("{id}/archive")]
    public Task<ExamDto> ArchiveAsync(Guid id) => _exams.ArchiveAsync(id);

    [HttpGet("{examId}/blueprint")]
    public Task<List<BlueprintRuleDto>> GetBlueprintAsync(Guid examId) => _exams.GetBlueprintAsync(examId);

    [HttpPut("{examId}/blueprint")]
    public Task<List<BlueprintRuleDto>> SetBlueprintAsync(
        Guid examId,
        [FromBody] List<CreateUpdateBlueprintRuleDto> rules) => _exams.SetBlueprintAsync(examId, rules);
}
