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
    /// <remarks>
    /// For a caller that genuinely holds the whole roll — a nightly sync out of a
    /// student record system, say. A browser does not hold it and must not
    /// pretend to; it uses <see cref="ChangeGroupMembersAsync"/> instead.
    /// </remarks>
    Task<CandidateGroupDto> SetGroupMembersAsync(Guid id, SetGroupMembersDto input);

    /// <summary>Puts these people into a cohort and takes those ones out.</summary>
    /// <remarks>
    /// The whole-list route above is only truthful while the caller can hold the
    /// whole list, and the screen cannot: it read 500 people, so a centre with
    /// more than that had candidates no coordinator could reach. Sending the
    /// change rather than the intended result needs no such holding, and takes
    /// the roll's size out of the protocol altogether.
    /// </remarks>
    Task<CandidateGroupDto> ChangeGroupMembersAsync(Guid id, ChangeGroupMembersDto input);
}
