using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People.Dtos;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// The people who sit exams, and the cohorts they belong to.
/// </summary>
/// <remarks>
/// Signed in at class level, and each method states its own permission.
/// <para>
/// It used to demand <c>Candidates</c> here as well. ASP.NET combines a class
/// and a method attribute with AND, not override — so a coordinator whose role
/// was "manage the classes, do not touch the candidate records" passed the route
/// guard, watched the screen mount, and had every request refused. The class
/// attribute is what keeps this off the anonymous surface; the method attributes
/// are what decide who may do what.
/// </para>
/// </remarks>
[Authorize]
public class CandidateAppService : ApplicationService, ICandidateAppService
{
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<CandidateGroup, Guid> _groups;
    private readonly IRepository<CandidateGroupMember, Guid> _members;
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<ExamLink, Guid> _links;
    private readonly IRepository<Category, Guid> _categories;
    private readonly IRepository<Level, Guid> _levels;

    public CandidateAppService(
        IRepository<Candidate, Guid> candidates,
        IRepository<CandidateGroup, Guid> groups,
        IRepository<CandidateGroupMember, Guid> members,
        IRepository<Attempt, Guid> attempts,
        IRepository<ExamLink, Guid> links,
        IRepository<Category, Guid> categories,
        IRepository<Level, Guid> levels)
    {
        _candidates = candidates;
        _groups = groups;
        _members = members;
        _attempts = attempts;
        _links = links;
        _categories = categories;
        _levels = levels;
    }

    // ------------------------------------------------------------- candidates

    [Authorize(InternshipManagementSystemPermissions.Candidates.View)]
    public async Task<PagedResultDto<CandidateDto>> GetListAsync(CandidateListRequestDto input)
    {
        var query = await _candidates.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            // Name, address and reference together: a coordinator looking somebody
            // up has whichever of the three they were given.
            var term = input.Filter.Trim();

            query = query.Where(c =>
                c.FullName.Contains(term) ||
                c.Email.Contains(term) ||
                (c.Reference != null && c.Reference.Contains(term)));
        }

        if (input.CategoryId is { } categoryId)
        {
            query = query.Where(c => c.CategoryId == categoryId);
        }

        if (input.Status is { } status)
        {
            // Derived, so the filter agrees with the column beside it. Both read
            // the same three facts: has a live link, has an unsubmitted attempt,
            // has a submitted one.
            var links = (await _links.GetQueryableAsync())
                .Where(l => !l.IsRevoked)
                .Select(l => l.CandidateId);

            var attempts = await _attempts.GetQueryableAsync();
            var running = attempts.Where(a => !a.IsSubmitted).Select(a => a.CandidateId);
            var finished = attempts.Where(a => a.IsSubmitted).Select(a => a.CandidateId);

            query = status switch
            {
                CandidateStatus.Completed => query.Where(c => finished.Contains(c.Id)),

                CandidateStatus.InProgress => query.Where(c =>
                    running.Contains(c.Id) && !finished.Contains(c.Id)),

                CandidateStatus.Invited => query.Where(c =>
                    links.Contains(c.Id) && !running.Contains(c.Id) && !finished.Contains(c.Id)),

                CandidateStatus.Pending => query.Where(c => !links.Contains(c.Id)),

                // Nothing records a withdrawal, so nothing can match one. An
                // empty page is the honest answer; quietly returning everybody
                // would be the old bug wearing a different hat.
                _ => query.Where(c => false),
            };
        }

        if (input.GroupId is { } groupId)
        {
            var memberIds = (await _members.GetQueryableAsync())
                .Where(m => m.CandidateGroupId == groupId)
                .Select(m => m.CandidateId);

            query = query.Where(c => memberIds.Contains(c.Id));
        }

        var totalCount = await query.CountAsync();

        var page = await query
            .OrderBy(c => c.FullName)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<CandidateDto>(totalCount, await ProjectAsync(page));
    }

    [Authorize(InternshipManagementSystemPermissions.Candidates.View)]
    public async Task<CandidateDto> GetAsync(Guid id)
    {
        var candidate = await _candidates.GetAsync(id);

        return (await ProjectAsync([candidate])).Single();
    }

    [Authorize(InternshipManagementSystemPermissions.Candidates.Create)]
    public async Task<CandidateDto> CreateAsync(CreateUpdateCandidateDto input)
    {
        await RequireFreeEmailAsync(input.Email, null);

        var candidate = new Candidate(GuidGenerator.Create(), CurrentTenant.Id, input.FullName, Normalise(input.Email));

        Apply(candidate, input);

        await _candidates.InsertAsync(candidate, autoSave: true);

        return await GetAsync(candidate.Id);
    }

    [Authorize(InternshipManagementSystemPermissions.Candidates.Edit)]
    public async Task<CandidateDto> UpdateAsync(Guid id, CreateUpdateCandidateDto input)
    {
        var candidate = await _candidates.GetAsync(id);

        await RequireFreeEmailAsync(input.Email, id);

        candidate.FullName = input.FullName;
        candidate.Email = Normalise(input.Email);
        Apply(candidate, input);

        await _candidates.UpdateAsync(candidate, autoSave: true);

        return await GetAsync(id);
    }

    [Authorize(InternshipManagementSystemPermissions.Candidates.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var sat = await (await _attempts.GetQueryableAsync()).AnyAsync(a => a.CandidateId == id);

        if (sat)
        {
            // Their results reference them. Deleting the person would leave a score
            // belonging to nobody, which is the one thing a result must never be.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CandidateHasAttempts);
        }

        await _candidates.DeleteAsync(id, autoSave: true);
    }

    // ----------------------------------------------------------------- import

    [Authorize(InternshipManagementSystemPermissions.Candidates.Create)]
    public async Task<ImportCandidatesResultDto> ImportAsync(ImportCandidatesDto input)
    {
        var result = new ImportCandidatesResultDto();

        var existing = await (await _candidates.GetQueryableAsync())
            .Select(c => new { c.Id, c.Email })
            .ToDictionaryAsync(c => c.Email, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var lines = input.Text.Replace("\r\n", "\n").Split('\n');
        var created = new List<Candidate>();
        var forGroup = new List<Guid>();

        // Emails seen in this paste, so a list that repeats somebody does not
        // create them twice within one import.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

            if (line.Length == 0)
            {
                continue;
            }

            var parsed = ParseLine(line);

            if (parsed.Problem is { } problem)
            {
                result.Problems.Add(new ImportProblemDto
                {
                    Line = index + 1,
                    Content = line,
                    Reason = problem,
                });

                continue;
            }

            var (fullName, email, phone, reference) = parsed.Person!.Value;

            if (existing.TryGetValue(email, out var alreadyId))
            {
                // Matched and left alone. Importing the same list twice must not
                // double the roll, and it must not overwrite a name somebody has
                // since corrected by hand.
                result.AlreadyPresent++;
                forGroup.Add(alreadyId);

                continue;
            }

            if (!seen.Add(email))
            {
                result.Problems.Add(new ImportProblemDto
                {
                    Line = index + 1,
                    Content = line,
                    Reason = "IMS:Import:RepeatedInThisList",
                });

                continue;
            }

            var candidate = new Candidate(GuidGenerator.Create(), CurrentTenant.Id, fullName, email)
            {
                PhoneNumber = phone,
                Reference = reference,
                CategoryId = input.CategoryId,
            };

            created.Add(candidate);
            forGroup.Add(candidate.Id);
            result.Created++;
        }

        if (input.DryRun)
        {
            // Counted and reported, nothing written. Somebody pasting forty rows
            // sees the three that are wrong before committing.
            result.AddedToGroup = input.GroupId is null ? 0 : forGroup.Count;

            return result;
        }

        if (created.Count > 0)
        {
            await _candidates.InsertManyAsync(created, autoSave: true);
        }

        if (input.GroupId is { } groupId && forGroup.Count > 0)
        {
            result.AddedToGroup = await AddToGroupAsync(groupId, forGroup);
        }

        return result;
    }

    // ---------------------------------------------------------------- cohorts

    [Authorize(InternshipManagementSystemPermissions.Groups.View)]
    public async Task<List<CandidateGroupDto>> GetGroupsAsync()
    {
        var groups = await (await _groups.GetQueryableAsync()).OrderBy(g => g.Name).ToListAsync();

        if (groups.Count == 0)
        {
            return [];
        }

        var counts = await (await _members.GetQueryableAsync())
            .GroupBy(m => m.CandidateGroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count);

        var categories = await LoadCategoriesAsync(groups.Select(g => g.CategoryId));
        var levels = await LoadLevelsAsync(groups.Select(g => g.LevelId));

        return groups
            .Select(group => new CandidateGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CategoryId = group.CategoryId,
                CategoryName = Name(categories, group.CategoryId),
                LevelId = group.LevelId,
                LevelName = Name(levels, group.LevelId),
                StartsOn = group.StartsOn,
                EndsOn = group.EndsOn,
                MemberCount = counts.GetValueOrDefault(group.Id),
                CreationTime = group.CreationTime,
            })
            .ToList();
    }

    [Authorize(InternshipManagementSystemPermissions.Groups.Create)]
    public async Task<CandidateGroupDto> CreateGroupAsync(CreateUpdateCandidateGroupDto input)
    {
        var group = new CandidateGroup(GuidGenerator.Create(), CurrentTenant.Id, input.Name)
        {
            Description = input.Description,
            CategoryId = input.CategoryId,
            LevelId = input.LevelId,
            StartsOn = input.StartsOn,
            EndsOn = input.EndsOn,
        };

        await _groups.InsertAsync(group, autoSave: true);

        return (await GetGroupsAsync()).First(g => g.Id == group.Id);
    }

    [Authorize(InternshipManagementSystemPermissions.Groups.Edit)]
    public async Task<CandidateGroupDto> UpdateGroupAsync(Guid id, CreateUpdateCandidateGroupDto input)
    {
        var group = await _groups.GetAsync(id);

        group.Name = input.Name;
        group.Description = input.Description;
        group.CategoryId = input.CategoryId;
        group.LevelId = input.LevelId;
        group.StartsOn = input.StartsOn;
        group.EndsOn = input.EndsOn;

        await _groups.UpdateAsync(group, autoSave: true);

        return (await GetGroupsAsync()).First(g => g.Id == id);
    }

    [Authorize(InternshipManagementSystemPermissions.Groups.Delete)]
    public async Task DeleteGroupAsync(Guid id)
    {
        var members = await (await _members.GetQueryableAsync())
            .Where(m => m.CandidateGroupId == id)
            .ToListAsync();

        // The cohort goes; the people in it do not. Deleting a heading must not
        // delete a roll of students.
        if (members.Count > 0)
        {
            await _members.DeleteManyAsync(members, autoSave: false);
        }

        await _groups.DeleteAsync(id, autoSave: true);
    }

    [Authorize(InternshipManagementSystemPermissions.Groups.Edit)]
    public async Task<CandidateGroupDto> SetGroupMembersAsync(Guid id, SetGroupMembersDto input)
    {
        await _groups.GetAsync(id);

        var existing = await (await _members.GetQueryableAsync())
            .Where(m => m.CandidateGroupId == id)
            .ToListAsync();

        // An empty list is not self-explanatory, and treating it as one cost a
        // class its whole roll. It means "remove everybody" only when the caller
        // says that is what they meant; otherwise it is a screen that failed to
        // read the class and does not know it.
        if (input.CandidateIds.Count == 0 && existing.Count > 0 && !input.ConfirmEmptied)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.GroupEmptyingNotConfirmed);
        }

        var wanted = input.CandidateIds.Distinct().ToHashSet();

        var removed = existing.Where(m => !wanted.Contains(m.CandidateId)).ToList();

        if (removed.Count > 0)
        {
            await _members.DeleteManyAsync(removed, autoSave: false);
        }

        var present = existing.Select(m => m.CandidateId).ToHashSet();

        await AddToGroupAsync(id, wanted.Where(candidateId => !present.Contains(candidateId)).ToList());

        return (await GetGroupsAsync()).First(g => g.Id == id);
    }

    /// <summary>
    /// Adds these people to a class and removes those ones.
    /// </summary>
    /// <remarks>
    /// The method above says "these people are the class", which is only a true
    /// sentence while the caller holds the class. The screen never did: it read
    /// 500 candidates and searched them in the browser, so at a centre with more
    /// than 500 people there were candidates that could not be found, could not
    /// be ticked, and therefore could not be put into any class at all — and
    /// raising the number only moves the wall, since ABP refuses a page over
    /// 1000 and a dialog of 1000 checkboxes is not a way to find one person.
    /// <para>
    /// Sending the change rather than the intended result takes the roll's size
    /// out of the protocol, and three defects stop existing rather than being
    /// guarded against. A roll longer than one page cannot be truncated by
    /// somebody editing it. A failed read produces an empty change, which does
    /// nothing, instead of an empty roll, which deleted classes. And two
    /// coordinators on the same class no longer overwrite each other: adding
    /// Fatima and adding Omar commute, so both survive, which is what worklist
    /// 6.1 asked for without needing a version stamp to detect a lost update
    /// that can no longer happen.
    /// </para>
    /// </remarks>
    [Authorize(InternshipManagementSystemPermissions.Groups.Edit)]
    public async Task<CandidateGroupDto> ChangeGroupMembersAsync(Guid id, ChangeGroupMembersDto input)
    {
        await _groups.GetAsync(id);

        var add = input.Add.Distinct().ToList();
        var remove = input.Remove.Distinct().ToHashSet();

        // Both at once is not an edit with an obvious winner, it is a caller that
        // has lost track of what it is asking for. Picking one silently would
        // make the roll depend on which half this method happens to apply first.
        var both = add.Where(remove.Contains).ToList();

        if (both.Count > 0)
        {
            throw new AbpValidationException(
                "A candidate cannot be added to and removed from the same class in one request.",
                [new ValidationResult($"Listed in both Add and Remove: {both[0]}.", [nameof(input.Add)])]);
        }

        if (add.Count > 0)
        {
            // Checked here rather than left to the foreign key, because a bad id
            // should come back as "no such candidate" and not as a database
            // error. The tenant filter is part of the check: an id belonging to
            // another organisation is simply not found, which is the correct
            // answer and leaks nothing.
            var known = await (await _candidates.GetQueryableAsync())
                .Where(c => add.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            var missing = add.Except(known).ToList();

            if (missing.Count > 0)
            {
                throw new EntityNotFoundException(typeof(Candidate), missing[0]);
            }
        }

        if (remove.Count > 0)
        {
            var rows = await (await _members.GetQueryableAsync())
                .Where(m => m.CandidateGroupId == id && remove.Contains(m.CandidateId))
                .ToListAsync();

            // Silent about the ones that were not in the class. Somebody removing
            // a person another coordinator removed a minute ago has got what they
            // wanted, and an error would be about the race rather than the roll.
            if (rows.Count > 0)
            {
                await _members.DeleteManyAsync(rows, autoSave: false);
            }
        }

        // Skips whoever is already in the class, for the same reason.
        await AddToGroupAsync(id, add);

        return (await GetGroupsAsync()).First(g => g.Id == id);
    }

    // ---------------------------------------------------------------- helpers

    private async Task<Dictionary<Guid, string>> LoadLevelsAsync(IEnumerable<Guid?> ids)
    {
        var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        if (wanted.Count == 0)
        {
            return [];
        }

        return await (await _levels.GetQueryableAsync())
            .Where(l => wanted.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name);
    }

    private async Task<int> AddToGroupAsync(Guid groupId, IReadOnlyCollection<Guid> candidateIds)
    {
        if (candidateIds.Count == 0)
        {
            return 0;
        }

        var already = await (await _members.GetQueryableAsync())
            .Where(m => m.CandidateGroupId == groupId)
            .Select(m => m.CandidateId)
            .ToListAsync();

        var toAdd = candidateIds
            .Distinct()
            .Where(candidateId => !already.Contains(candidateId))
            .Select(candidateId => new CandidateGroupMember(
                GuidGenerator.Create(), CurrentTenant.Id, groupId, candidateId))
            .ToList();

        if (toAdd.Count > 0)
        {
            await _members.InsertManyAsync(toAdd, autoSave: true);
        }

        return toAdd.Count;
    }

    private async Task RequireFreeEmailAsync(string email, Guid? excluding)
    {
        var normalised = Normalise(email);

        var taken = await (await _candidates.GetQueryableAsync())
            .AnyAsync(c => c.Email == normalised && (excluding == null || c.Id != excluding));

        if (taken)
        {
            // The address is how a link reaches them and how an import recognises
            // somebody already on the roll. Two people sharing one makes both
            // ambiguous.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CandidateEmailTaken);
        }
    }

    /// <summary>
    /// How far along one person is, from the three facts that actually record it.
    /// <para>
    /// Most advanced wins: somebody who has sat once and holds a second live
    /// link is Completed, not Invited. A coordinator scanning a roll is looking
    /// for who still has to sit, and burying that under a later invitation is
    /// the thing that makes a status column useless.
    /// </para>
    /// </summary>
    private static CandidateStatus StatusOf(
        Guid id,
        IReadOnlySet<Guid> invited,
        IReadOnlySet<Guid> running,
        IReadOnlySet<Guid> finished)
    {
        if (finished.Contains(id)) return CandidateStatus.Completed;
        if (running.Contains(id)) return CandidateStatus.InProgress;
        if (invited.Contains(id)) return CandidateStatus.Invited;

        return CandidateStatus.Pending;
    }

    private async Task<List<CandidateDto>> ProjectAsync(IReadOnlyCollection<Candidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var ids = candidates.Select(c => c.Id).ToList();

        var memberships = await (await _members.GetQueryableAsync())
            .Where(m => ids.Contains(m.CandidateId))
            .ToListAsync();

        var groupIds = memberships.Select(m => m.CandidateGroupId).Distinct().ToList();

        var groupNames = groupIds.Count == 0
            ? []
            : await (await _groups.GetQueryableAsync())
                .Where(g => groupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name);

        var attemptRows = await (await _attempts.GetQueryableAsync())
            .Where(a => ids.Contains(a.CandidateId))
            .Select(a => new { a.CandidateId, a.IsSubmitted })
            .ToListAsync();

        var attemptCounts = attemptRows
            .GroupBy(a => a.CandidateId)
            .ToDictionary(g => g.Key, g => g.Count());

        var finished = attemptRows.Where(a => a.IsSubmitted).Select(a => a.CandidateId).ToHashSet();
        var running = attemptRows.Where(a => !a.IsSubmitted).Select(a => a.CandidateId).ToHashSet();

        var invited = (await (await _links.GetQueryableAsync())
            .Where(l => ids.Contains(l.CandidateId) && !l.IsRevoked)
            .Select(l => l.CandidateId)
            .ToListAsync())
            .ToHashSet();

        var categories = await LoadCategoriesAsync(candidates.Select(c => c.CategoryId));

        return candidates
            .Select(candidate => new CandidateDto
            {
                Id = candidate.Id,
                FullName = candidate.FullName,
                Email = candidate.Email,
                PhoneNumber = candidate.PhoneNumber,
                CategoryId = candidate.CategoryId,
                CategoryName = Name(categories, candidate.CategoryId),
                Reference = candidate.Reference,
                Status = StatusOf(candidate.Id, invited, running, finished),
                GroupNames = memberships
                    .Where(m => m.CandidateId == candidate.Id)
                    .Select(m => groupNames.GetValueOrDefault(m.CandidateGroupId))
                    .Where(name => name is not null)
                    .Select(name => name!)
                    .ToList(),
                AttemptCount = attemptCounts.GetValueOrDefault(candidate.Id),
                CreationTime = candidate.CreationTime,
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, string>> LoadCategoriesAsync(IEnumerable<Guid?> ids)
    {
        var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        if (wanted.Count == 0)
        {
            return [];
        }

        return await (await _categories.GetQueryableAsync())
            .Where(c => wanted.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);
    }

    private static string? Name(IReadOnlyDictionary<Guid, string> lookup, Guid? id) =>
        id is { } value && lookup.TryGetValue(value, out var name) ? name : null;

    private static void Apply(Candidate candidate, CreateUpdateCandidateDto input)
    {
        candidate.PhoneNumber = input.PhoneNumber;
        candidate.CategoryId = input.CategoryId;
        candidate.Reference = input.Reference;
    }

    private static string Normalise(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Reads one pasted line.
    /// <para>
    /// Comma or tab, because a paste straight out of a spreadsheet arrives with
    /// tabs and a paste out of a document arrives with commas, and asking somebody
    /// which one they have is asking them to know something they should not need to.
    /// </para>
    /// </summary>
    private static ParsedLine ParseLine(string line)
    {
        var parts = line
            .Split(['	', ','], StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .ToList();

        if (parts.Count < 2)
        {
            return new ParsedLine { Problem = "IMS:Import:NeedsNameAndEmail" };
        }

        // The address is found rather than assumed to be second: some rolls are
        // written email-first, and refusing those would be refusing a valid list
        // over a column order.
        var emailIndex = parts.FindIndex(LooksLikeEmail);

        if (emailIndex < 0)
        {
            // A complete-looking row whose address is not one. Told apart from a
            // short row on purpose: "there is no address on this line" and "this
            // line is missing a column" send somebody to different places.
            return new ParsedLine { Problem = "IMS:Import:NotAnEmail" };
        }

        var email = parts[emailIndex].ToLowerInvariant();
        var rest = parts.Where((_, i) => i != emailIndex).ToList();

        return new ParsedLine
        {
            Person = (rest[0], email, rest.Count > 1 ? rest[1] : null, rest.Count > 2 ? rest[2] : null),
        };
    }

    /// <summary>One pasted line, read: either a person or the reason it is not.</summary>
    private readonly struct ParsedLine
    {
        public string? Problem { get; init; }

        public (string FullName, string Email, string? Phone, string? Reference)? Person { get; init; }
    }

    /// <summary>
    /// Deliberately loose. Rejecting a real address because it does not match a
    /// clever pattern is worse than accepting one that later bounces, and the
    /// bounce is visible where this is not.
    /// </summary>
    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');

        return at > 0 && at < value.Length - 1 && value.IndexOf('.', at) > at + 1;
    }
}
