using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Review;
using InternshipManagementSystem.Assessment.Review.Dtos;
using InternshipManagementSystem.Permissions;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// The permissions a marker and a coordinator hold, executed rather than inspected.
/// <para>
/// <c>AuthorizationCoverageTests</c> reads the assembly and asks whether an
/// <c>[Authorize]</c> attribute exists. It cannot ask whether it works, because the
/// suite it belongs to runs under <c>AddAlwaysAllowAuthorization</c>. Worse, its
/// <c>EnforcedInCode</c> escape hatch matches on the last segment of a permission
/// name across the whole Application source tree, so <c>.View)</c> occurring
/// anywhere keeps all nine <c>*.View</c> permissions looking enforced: deleting
/// <c>[Authorize(Attempts.View)]</c> and <c>[Authorize(Attempts.Delete)]</c> leaves
/// the whole backend suite green.
/// </para>
/// <para>
/// These call the methods. Every one is a pair — refused without the permission,
/// allowed with it — because a refusal on its own is satisfied by a service that is
/// broken for an unrelated reason, and an allowance on its own is satisfied by no
/// guard at all.
/// </para>
/// <para>
/// The refusing half grants <em>everything except</em> the permission under test.
/// That is deliberate and stronger than granting nothing: it says this permission,
/// and only this permission, is what stands between the caller and the operation.
/// </para>
/// </summary>
public class GuardedByAttributeTests : AssessmentPermissionTestBase
{
    private readonly IReviewAppService _review;
    private readonly IAttemptAdminAppService _admin;

    public GuardedByAttributeTests()
    {
        _review = GetRequiredService<IReviewAppService>();
        _admin = GetRequiredService<IAttemptAdminAppService>();
    }

    // ------------------------------------------------------------- Review.Grade

    [Fact]
    public async Task Awarding_a_mark_without_Review_Grade_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("grade-refused");
            var sitting = await SatAndSubmittedAsync(exam, "grade-refused");
            var answer = (await _review.GetAnswersAsync(sitting.AttemptId)).Single();

            // Everything a marker could hold, minus the one that awards marks.
            GrantEverythingExcept(InternshipManagementSystemPermissions.Review.Grade);

            await RefusedAsync(() => _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = answer.AnswerId,
                AwardedScore = 17m,
            }));
        });
    }

    [Fact]
    public async Task Awarding_a_mark_with_Review_Grade_succeeds()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("grade-allowed");
            var sitting = await SatAndSubmittedAsync(exam, "grade-allowed");
            var answer = (await _review.GetAnswersAsync(sitting.AttemptId)).Single();

            // Exactly the chain the service names: the class guard and the method
            // guard, and nothing else. If either link were missing this would be
            // refused, which is what makes the pair meaningful.
            GrantOnly(
                InternshipManagementSystemPermissions.Review.Default,
                InternshipManagementSystemPermissions.Review.Grade,
                InternshipManagementSystemPermissions.Review.ViewQueue);

            await _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = answer.AnswerId,
                AwardedScore = 17m,
            });

            // Through the attempt rather than the review queue: a marked answer
            // leaves the queue, so re-reading it there would assert nothing.
            var attempts = GetRequiredService<IRepository<Attempt, Guid>>();

            (await attempts.GetAsync(sitting.AttemptId)).Score.ShouldBe(17m);
        });
    }

    [Fact]
    public async Task The_class_guard_is_enforced_as_well_as_the_method_guard()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("grade-class");
            var sitting = await SatAndSubmittedAsync(exam, "grade-class");
            var answer = (await _review.GetAnswersAsync(sitting.AttemptId)).Single();

            // Holds the permission that names the operation, but not the one on the
            // class. ABP grants are one row per name — holding a child does not
            // imply holding its parent — so a role built by granting only the leaf
            // is refused, and this documents that as behaviour rather than folklore.
            GrantOnly(InternshipManagementSystemPermissions.Review.Grade);

            await RefusedAsync(() => _review.GradeAnswerAsync(new GradeAnswerDto
            {
                AnswerId = answer.AnswerId,
                AwardedScore = 17m,
            }));
        });
    }

    // ------------------------------------------------------------ Attempts.View

    [Fact]
    public async Task Seeing_a_sitting_in_progress_without_Attempts_View_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("running-refused");
            await StartedSittingAsync(exam, "running-refused");

            GrantEverythingExcept(InternshipManagementSystemPermissions.Attempts.View);

            await RefusedAsync(() => _admin.GetRunningAsync(new RunningAttemptRequestDto()));
        });
    }

    [Fact]
    public async Task Seeing_a_sitting_in_progress_with_Attempts_View_succeeds()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("running-allowed");
            var sitting = await StartedSittingAsync(exam, "running-allowed");

            // Results.View is in this list because the implementation needs it,
            // not because the permission tree says so: GetRunningAsync builds each
            // row by calling ResultAppService.GetAsync, whose class carries
            // [Authorize(Results.View)]. A role granted Attempts.View alone — which
            // is what the administration screen offers — is refused. Worth a
            // decision; recorded here because nothing else in the repository can
            // see it.
            GrantOnly(
                InternshipManagementSystemPermissions.Attempts.Default,
                InternshipManagementSystemPermissions.Attempts.View,
                InternshipManagementSystemPermissions.Results.View);

            var running = await _admin.GetRunningAsync(new RunningAttemptRequestDto());

            // Non-empty on purpose. A permitted call that returns nothing cannot be
            // told from a permitted call that failed to reach the data.
            running.Items.ShouldContain(item => item.AttemptId == sitting.AttemptId);
        });
    }

    // ----------------------------------------------------- Attempts.ForceSubmit

    [Fact]
    public async Task Ending_someone_elses_sitting_without_Attempts_ForceSubmit_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("force-refused");
            var sitting = await StartedSittingAsync(exam, "force-refused");

            GrantEverythingExcept(InternshipManagementSystemPermissions.Attempts.ForceSubmit);

            await RefusedAsync(() => _admin.ForceSubmitAsync(
                sitting.AttemptId,
                new ForceSubmitDto { Reason = "Their browser froze." }));
        });
    }

    [Fact]
    public async Task Ending_someone_elses_sitting_with_Attempts_ForceSubmit_succeeds()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("force-allowed");
            var sitting = await StartedSittingAsync(exam, "force-allowed");

            // Results.View for the same reason as above: the row it returns is
            // built through ResultAppService.
            GrantOnly(
                InternshipManagementSystemPermissions.Attempts.Default,
                InternshipManagementSystemPermissions.Attempts.ForceSubmit,
                InternshipManagementSystemPermissions.Results.View);

            var row = await _admin.ForceSubmitAsync(
                sitting.AttemptId,
                new ForceSubmitDto { Reason = "Their browser froze." });

            row.AttemptId.ShouldBe(sitting.AttemptId);
        });
    }

    // ---------------------------------------------------------- Attempts.Delete

    [Fact]
    public async Task Deleting_a_sitting_without_Attempts_Delete_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("delete-refused");
            var sitting = await SatAndSubmittedAsync(exam, "delete-refused");

            GrantEverythingExcept(InternshipManagementSystemPermissions.Attempts.Delete);

            await RefusedAsync(() => _admin.DeleteAsync(sitting.AttemptId));
        });
    }

    [Fact]
    public async Task Deleting_a_sitting_with_Attempts_Delete_succeeds()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("delete-allowed");
            var sitting = await SatAndSubmittedAsync(exam, "delete-allowed");

            var attempts = GetRequiredService<IRepository<Attempt, Guid>>();

            // Present first. Asserting only that it is gone afterwards would also
            // pass if it had never been written.
            (await attempts.FindAsync(sitting.AttemptId)).ShouldNotBeNull();

            GrantOnly(
                InternshipManagementSystemPermissions.Attempts.Default,
                InternshipManagementSystemPermissions.Attempts.Delete);

            await _admin.DeleteAsync(sitting.AttemptId);

            (await attempts.FindAsync(sitting.AttemptId)).ShouldBeNull();
        });
    }
}
