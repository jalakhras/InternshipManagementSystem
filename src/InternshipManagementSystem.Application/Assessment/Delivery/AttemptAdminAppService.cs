using System;
using Microsoft.Extensions.Logging;
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
using Volo.Abp.BlobStoring;
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
    private readonly IRepository<IntegritySignal, Guid> _signals;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IBlobContainer<AssessmentBlobContainer> _blobs;
    private readonly IResultAppService _results;
    private readonly AttemptGradingService _grading;

    public AttemptAdminAppService(
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<IntegritySignal, Guid> signals,
        IRepository<Candidate, Guid> candidates,
        IBlobContainer<AssessmentBlobContainer> blobs,
        IResultAppService results,
        AttemptGradingService grading)
    {
        _attempts = attempts;
        _attemptQuestions = attemptQuestions;
        _answers = answers;
        _signals = signals;
        _candidates = candidates;
        _blobs = blobs;
        _results = results;
        _grading = grading;
    }

    /// <summary>
    /// Both, and the second is not redundant.
    /// <para>
    /// Every row here is a result summary, built by the results service so that
    /// a running sitting and a finished one are described by the same code. So
    /// watching sittings really does mean seeing results, and the class-level
    /// guard on that service enforces it whether this attribute says so or not.
    /// </para>
    /// <para>
    /// Written down because it was invisible: a role granted <c>Attempts.View</c>
    /// alone — which is exactly what the permission screen offers — was refused
    /// at the moment of use, by a guard on a service it never asked for. A
    /// requirement the product enforces and does not state is a requirement
    /// nobody can satisfy on purpose.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Attempts.View)]
    [Authorize(InternshipManagementSystemPermissions.Results.View)]
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

            // The one definition, so this box and the roll's box answer the
            // same question. These three searched the raw name only, which meant
            // a person found on one screen could not be found on the next.
            var candidateIds = (await _candidates.GetQueryableAsync())
                .Where(CandidateSearch.Matching(term))
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

    /// <summary>Requires seeing results too, for the reason given above.</summary>
    [Authorize(InternshipManagementSystemPermissions.Attempts.ForceSubmit)]
    [Authorize(InternshipManagementSystemPermissions.Results.View)]
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

        // What was observed about the person while they sat: what they pasted,
        // when they left the window, how long they took.
        //
        // These are the recordings the dialog means when it says everything the
        // attempt recorded is removed, and they were the one thing never deleted
        // anywhere in the product. They outlived the sitting they describe,
        // pointing at an attempt that no longer existed — observations about
        // somebody that nothing left could explain.
        var observations = await (await _signals.GetQueryableAsync())
            .Where(signal => signal.AttemptId == attemptId)
            .ToListAsync();

        // Read before the rows go, because a file's address lives on the row
        // that refers to it and a container cannot be listed by prefix.
        var files = answers
            .Select(answer => answer.AnswerBlobName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        await _signals.DeleteManyAsync(observations, autoSave: true);
        await _answers.DeleteManyAsync(answers, autoSave: true);
        await _attemptQuestions.DeleteManyAsync(slots, autoSave: true);
        await _attempts.DeleteAsync(attempt, autoSave: true);

        await RemoveFilesAsync(files!, attemptId);
    }

    /// <summary>
    /// The recordings and uploads this attempt left in storage.
    /// <para>
    /// A candidate's spoken answer is the most personal thing this product
    /// holds. Deleting the row that names the file and leaving the file is worse
    /// than not deleting at all: the recording stays on disk and nothing is left
    /// that could find it again to finish the job.
    /// </para>
    /// <para>
    /// One failure does not stop the rest, and whatever will not go is named in
    /// the log so a person can remove it by hand.
    /// </para>
    /// </summary>
    private async Task RemoveFilesAsync(System.Collections.Generic.List<string> names, Guid attemptId)
    {
        foreach (var name in names)
        {
            try
            {
                await _blobs.DeleteAsync(name);
            }
            catch (Exception failure)
            {
                Logger.LogWarning(
                    failure,
                    "Could not remove {Blob} from discarded attempt {AttemptId}. "
                    + "It must be removed by hand: the row that named it has gone.",
                    name,
                    attemptId);
            }
        }
    }
}
