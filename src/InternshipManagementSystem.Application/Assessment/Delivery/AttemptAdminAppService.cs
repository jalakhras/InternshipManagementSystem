using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.Results;
using InternshipManagementSystem.Assessment.Results.Dtos;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Attempts in progress, and what a coordinator can do about one.
/// <para>
/// Three permissions described this and nothing implemented them. What they
/// describe is real: somebody is in the room, their browser has frozen, and the
/// coordinator can see neither that the attempt is live nor any way to end it.
/// </para>
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Attempts.Default)]
public class AttemptAdminAppService : ApplicationService, IAttemptAdminAppService
{
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestions;
    private readonly IRepository<Answer, Guid> _answers;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IResultAppService _results;
    private readonly AttemptGradingService _grading;

    public AttemptAdminAppService(
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<Candidate, Guid> candidates,
        IResultAppService results,
        AttemptGradingService grading)
    {
        _attempts = attempts;
        _attemptQuestions = attemptQuestions;
        _answers = answers;
        _candidates = candidates;
        _results = results;
        _grading = grading;
    }

    [Authorize(InternshipManagementSystemPermissions.Attempts.View)]
    public async Task<PagedResultDto<ResultRowDto>> GetRunningAsync(RunningAttemptRequestDto input)
    {
        var now = Clock.Now;
        var query = (await _attempts.GetQueryableAsync()).Where(a => !a.IsSubmitted);

        if (input.IncludeExpired != true)
        {
            // Past its deadline and not yet closed is a state that lasts under a
            // minute — the timeout worker is already on it. Listing those invites
            // somebody to intervene where they need not.
            query = query.Where(a => a.DeadlineAt >= now);
        }

        if (input.ExamId is { } examId)
        {
            query = query.Where(a => a.ExamId == examId);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim();

            var candidateIds = (await _candidates.GetQueryableAsync())
                .Where(c => c.FullName.Contains(term) || c.Email.Contains(term))
                .Select(c => c.Id);

            query = query.Where(a => candidateIds.Contains(a.CandidateId));
        }

        var totalCount = await query.CountAsync();

        var ids = await query
            // The one that started most recently is the one somebody is asking
            // about, because they are standing next to them.
            .OrderByDescending(a => a.StartedAt)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .Select(a => a.Id)
            .ToListAsync();

        var rows = new System.Collections.Generic.List<ResultRowDto>();

        foreach (var id in ids)
        {
            // Through the results service so a running sitting and a finished one
            // are described by the same code. Two shapes for the same row is how
            // one of them ends up saying something the other does not.
            rows.Add((await _results.GetAsync(id)).Summary);
        }

        return new PagedResultDto<ResultRowDto>(totalCount, rows);
    }

    [Authorize(InternshipManagementSystemPermissions.Attempts.ForceSubmit)]
    public async Task<ResultRowDto> ForceSubmitAsync(Guid attemptId, ForceSubmitDto input)
    {
        var attempt = await _attempts.GetAsync(attemptId);

        if (attempt.IsSubmitted)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptAlreadySubmitted);
        }

        attempt.IsSubmitted = true;
        attempt.SubmittedAt = Clock.Now;
        attempt.EndReason = AttemptEndReason.EndedByAdministrator;
        attempt.EndedByReason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim();

        await _attempts.UpdateAsync(attempt, autoSave: true);

        // Everything answered before this moment counts in full. The candidate did
        // that work, and the reason they stopped is not their score's problem.
        await _grading.GradeAsync(attempt.Id);

        return (await _results.GetAsync(attempt.Id)).Summary;
    }

    [Authorize(InternshipManagementSystemPermissions.Attempts.Delete)]
    public async Task DeleteAsync(Guid attemptId)
    {
        var attempt = await _attempts.GetAsync(attemptId);

        if (attempt.IsGraded)
        {
            // A graded attempt is somebody's result. Removing one is not a
            // correction, it is a disappearance — and the person who sat it has
            // no way to know it happened. Ending it early is available and is
            // recorded; deleting it is not.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptGradedCannotDelete);
        }

        var answers = await (await _answers.GetQueryableAsync())
            .Where(a => a.AttemptId == attemptId)
            .ToListAsync();

        var slots = await (await _attemptQuestions.GetQueryableAsync())
            .Where(q => q.AttemptId == attemptId)
            .ToListAsync();

        await _answers.DeleteManyAsync(answers, autoSave: true);
        await _attemptQuestions.DeleteManyAsync(slots, autoSave: true);
        await _attempts.DeleteAsync(attempt, autoSave: true);
    }
}
