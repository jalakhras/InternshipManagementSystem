using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Grading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Submits attempts whose deadline has passed.
/// <para>
/// The browser also submits when its timer reaches zero, but that only covers the
/// case where a browser is still there. A closed laptop, a dead battery or a lost
/// connection would otherwise leave the attempt open forever: never graded, never
/// releasing the link's attempt count, and sitting in every operator's list as
/// permanently in progress.
/// </para>
/// <para>
/// Runs against stored deadlines, so it needs no cooperation from the client and
/// cannot be talked out of firing by a tampered clock.
/// </para>
/// </summary>
public class AttemptTimeoutWorker : AsyncPeriodicBackgroundWorkerBase
{
    public AttemptTimeoutWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        // A minute is close enough: the deadline itself is what counts, and a taker
        // who submitted normally never reaches this path.
        Timer.Period = 60 * 1000;
    }

    /// <summary>Shared across instances, so only one of them sweeps at a time.</summary>
    private const string DistributedLockName = "IMS_AttemptTimeoutSweep";

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var attempts = workerContext.ServiceProvider.GetRequiredService<IRepository<Attempt, Guid>>();
        var grading = workerContext.ServiceProvider.GetRequiredService<AttemptGradingService>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var dataFilter = workerContext.ServiceProvider.GetRequiredService<IDataFilter>();
        var distributedLock = workerContext.ServiceProvider.GetRequiredService<IAbpDistributedLock>();

        // Behind a load balancer this worker runs on every instance, and two of them
        // reaching the same expired attempt would grade it twice. Taking the lock
        // keeps the deployment shape a free choice: one box or ten, same behaviour.
        // A handle of null means another instance already holds it, which is not an
        // error — the work is being done.
        await using var handle = await distributedLock.TryAcquireAsync(DistributedLockName);

        if (handle is null)
        {
            return;
        }

        var now = clock.Now;

        // Deliberately crosses tenants: this is host-level housekeeping, and every
        // tenant's expired attempts need closing. Nothing tenant-specific is read
        // beyond the attempt itself.
        using (dataFilter.Disable<Volo.Abp.MultiTenancy.IMultiTenant>())
        {
            var queryable = await attempts.GetQueryableAsync();

            var expired = await queryable
                .Where(a => !a.IsSubmitted && a.DeadlineAt < now)
                .OrderBy(a => a.DeadlineAt)
                .Take(100)
                .ToListAsync();

            if (expired.Count == 0)
            {
                return;
            }

            Logger.LogInformation("Auto-submitting {Count} attempt(s) past their deadline.", expired.Count);

            foreach (var attempt in expired)
            {
                attempt.IsSubmitted = true;
                attempt.SubmittedAt = attempt.DeadlineAt;
                attempt.EndReason = AttemptEndReason.TimedOutOnServer;

                await attempts.UpdateAsync(attempt, autoSave: true);

                try
                {
                    // Answers saved before the deadline count in full: the taker did
                    // the work, and the reason they stopped is not their score's problem.
                    await grading.GradeAsync(attempt.Id);
                }
                catch (Exception ex)
                {
                    // One bad attempt must not stall the queue for everyone else.
                    Logger.LogError(ex, "Failed to grade timed-out attempt {AttemptId}.", attempt.Id);
                }
            }
        }
    }
}
