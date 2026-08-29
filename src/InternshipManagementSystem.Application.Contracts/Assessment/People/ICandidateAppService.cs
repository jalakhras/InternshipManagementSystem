using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// The people who sit exams, and the cohorts they are grouped into.
/// <para>
/// A candidate is not a user. They have no account, no password and nothing to
/// log in to — they are a name, an address to send a link to, and whatever
/// reference the organisation already knows them by. That decision is what makes
/// the taking flow possible at all, and it is why this lives beside the
/// assessment rather than inside identity.
/// </para>
/// </summary>
public interface ICandidateAppService : IApplicationService
{
    Task<PagedResultDto<CandidateDto>> GetListAsync(CandidateListRequestDto input);

    Task<CandidateDto> GetAsync(Guid id);

    Task<CandidateDto> CreateAsync(CreateUpdateCandidateDto input);

    Task<CandidateDto> UpdateAsync(Guid id, CreateUpdateCandidateDto input);

    Task DeleteAsync(Guid id);

    /// <summary>
    /// Reads a pasted list and creates whoever is not already there.
    /// <para>
    /// With <c>DryRun</c> it reports what would happen and writes nothing, so a
    /// person pasting forty rows sees the three that are wrong before committing
    /// rather than afterwards.
    /// </para>
    /// </summary>
    Task<ImportCandidatesResultDto> ImportAsync(ImportCandidatesDto input);

    // --------------------------------------------------------------- cohorts

    Task<List<CandidateGroupDto>> GetGroupsAsync();

    Task<CandidateGroupDto> CreateGroupAsync(CreateUpdateCandidateGroupDto input);

    Task<CandidateGroupDto> UpdateGroupAsync(Guid id, CreateUpdateCandidateGroupDto input);

    Task DeleteGroupAsync(Guid id);

    /// <summary>Replaces a cohort's membership with exactly these people.</summary>
    Task<CandidateGroupDto> SetGroupMembersAsync(Guid id, SetGroupMembersDto input);

    /// <summary>
    /// Sets which papers this class sits, in the order given.
    /// <para>
    /// The order is the point: the first is what everyone sits, the second is
    /// what a retake uses. That is what turns the retake guarantee from a
    /// property of the schema into a decision somebody made.
    /// </para>
    /// </summary>
    Task<CandidateGroupDto> SetGroupFormsAsync(Guid id, SetGroupFormsDto input);
}
