using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// Candidates and the cohorts they belong to.
/// <para>
/// Personal data, so every route is guarded including the reads. A candidate is
/// not a user of this system and never becomes one: they are a name and an
/// address to send a link to.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/candidates")]
public class CandidateController : AbpControllerBase
{
    private readonly ICandidateAppService _candidates;

    public CandidateController(ICandidateAppService candidates)
    {
        _candidates = candidates;
    }

    [HttpGet]
    public Task<PagedResultDto<CandidateDto>> GetListAsync([FromQuery] CandidateListRequestDto input) =>
        _candidates.GetListAsync(input);

    /// <summary>Declared before the {id} route so "groups" is not read as an id.</summary>
    [HttpGet("groups")]
    public Task<List<CandidateGroupDto>> GetGroupsAsync() => _candidates.GetGroupsAsync();

    [HttpPost("groups")]
    public Task<CandidateGroupDto> CreateGroupAsync([FromBody] CreateUpdateCandidateGroupDto input) =>
        _candidates.CreateGroupAsync(input);

    [HttpPut("groups/{id}")]
    public Task<CandidateGroupDto> UpdateGroupAsync(Guid id, [FromBody] CreateUpdateCandidateGroupDto input) =>
        _candidates.UpdateGroupAsync(id, input);

    [HttpDelete("groups/{id}")]
    public Task DeleteGroupAsync(Guid id) => _candidates.DeleteGroupAsync(id);

    /// <summary>The whole roll, replaced. For a caller that actually holds it.</summary>
    [HttpPut("groups/{id}/members")]
    public Task<CandidateGroupDto> SetGroupMembersAsync(Guid id, [FromBody] SetGroupMembersDto input) =>
        _candidates.SetGroupMembersAsync(id, input);

    /// <summary>
    /// A change to the roll: add these, take those out.
    /// <para>
    /// POST rather than PUT because the body is what changed, not what the roll
    /// should end up being — and that is the point. A browser cannot hold a roll
    /// of any size, and the screen above used to claim it could.
    /// </para>
    /// </summary>
    [HttpPost("groups/{id}/members")]
    public Task<CandidateGroupDto> ChangeGroupMembersAsync(Guid id, [FromBody] ChangeGroupMembersDto input) =>
        _candidates.ChangeGroupMembersAsync(id, input);

    [HttpPost("import")]
    public Task<ImportCandidatesResultDto> ImportAsync([FromBody] ImportCandidatesDto input) =>
        _candidates.ImportAsync(input);

    [HttpGet("{id}")]
    public Task<CandidateDto> GetAsync(Guid id) => _candidates.GetAsync(id);

    [HttpPost]
    public Task<CandidateDto> CreateAsync([FromBody] CreateUpdateCandidateDto input) =>
        _candidates.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<CandidateDto> UpdateAsync(Guid id, [FromBody] CreateUpdateCandidateDto input) =>
        _candidates.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => _candidates.DeleteAsync(id);
}
