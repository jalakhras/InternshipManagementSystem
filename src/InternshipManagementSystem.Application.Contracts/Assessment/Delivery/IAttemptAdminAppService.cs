using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Results.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Attempts that are still running, and what a coordinator can do about one.
/// <para>
/// <c>Attempts.View</c>, <c>.ForceSubmit</c> and <c>.Delete</c> were three
/// grantable permissions that enforced nothing, because nothing implemented
/// them. What they describe is a real need: somebody is in the room, their
/// browser has frozen, and the coordinator needs to see that the attempt is
/// live and end it — or clear a test run that should never have counted.
/// </para>
/// </summary>
public interface IAttemptAdminAppService : IApplicationService
{
    /// <summary>
    /// Sittings in progress right now, most recently started first.
    /// <para>
    /// Reuses the result row rather than inventing a second shape for the same
    /// thing. A running attempt has no score yet and says so.
    /// </para>
    /// </summary>
    Task<PagedResultDto<ResultRowDto>> GetRunningAsync(RunningAttemptRequestDto input);

    /// <summary>
    /// Ends a sitting now and marks it, on the record, as ended by a person.
    /// <para>
    /// Whatever was answered before this moment counts in full. The candidate did
    /// that work, and the reason they stopped is not their score's problem.
    /// </para>
    /// </summary>
    Task<ResultRowDto> ForceSubmitAsync(Guid attemptId, ForceSubmitDto input);

    /// <summary>
    /// Removes an attempt and everything it recorded.
    /// <para>
    /// For a test run, a duplicate, or a sitting that should never have started.
    /// Not for a result somebody dislikes: a graded attempt is refused, because
    /// deleting a real score is not a correction, it is a disappearance.
    /// </para>
    /// </summary>
    Task DeleteAsync(Guid attemptId);
}

public class RunningAttemptRequestDto : PagedAndSortedResultRequestDto
{
    public Guid? ExamId { get; set; }

    /// <summary>Name or email.</summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Include sittings whose deadline has passed but which the timeout worker
    /// has not closed yet. Off by default: those close themselves within a
    /// minute, and listing them invites somebody to intervene where they need not.
    /// </summary>
    public bool? IncludeExpired { get; set; }
}

public class ForceSubmitDto
{
    /// <summary>
    /// Why, in the coordinator's words.
    /// <para>
    /// Recorded because ending somebody's exam early is the kind of act that gets
    /// questioned weeks later, and "the system did it" is not an answer anybody
    /// can defend.
    /// </para>
    /// </summary>
    public string? Reason { get; set; }
}
