using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.People.Dtos;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Results;
using InternshipManagementSystem.Assessment.Results.Dtos;
using InternshipManagementSystem.IdentityManagement;
using InternshipManagementSystem.IdentityManagement.DTOs;
using InternshipManagementSystem.Permissions;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// The three decisions no attribute carries, and no static check can find.
/// <para>
/// <c>AuthorizationCoverageTests.Every_defined_permission_is_enforced_somewhere</c>
/// looks for the permission's last segment as a substring of the Application source.
/// For <c>Assignments.SendEmail</c> the string that keeps it alive is
/// <c>if (input.SendEmail)</c> — a DTO property read, not an authorisation check —
/// so the permission is, in that test's own terms, permanently uncatchable.
/// <c>Users.ManageRoles</c> and <c>Review.ViewIntegritySignals</c> are enforced by
/// explicit calls to <c>AuthorizationService</c>, which the same substring match
/// cannot distinguish from any other mention.
/// </para>
/// <para>
/// Two of the three are conditional, and the condition is the interesting part: the
/// permission must be required when the thing it guards is being done, and must
/// <em>not</em> be required otherwise, or the two permissions collapse into one and
/// the separation the organisation bought disappears. Each is tested both ways.
/// </para>
/// </summary>
public class GuardedInCodeTests : AssessmentPermissionTestBase
{
    // -------------------------------------------------- Assignments.SendEmail

    [Fact]
    public async Task Sending_the_link_by_email_without_Assignments_SendEmail_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("send-refused");

            var candidate = await Candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Mailed",
                Email = "send-refused@example.test",
            });

            GrantEverythingExcept(InternshipManagementSystemPermissions.Assignments.SendEmail);

            await RefusedAsync(() => Assignments.CreateAsync(new CreateAssignmentDto
            {
                ExamId = exam,
                CandidateId = candidate.Id,
                ExpiresAt = DateTime.Now.AddDays(7),
                MaxAttempts = 1,
                SendEmail = true,
            }));
        });
    }

    [Fact]
    public async Task Preparing_a_sitting_without_sending_it_does_not_need_Assignments_SendEmail()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("send-not-needed");

            var candidate = await Candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Not mailed",
                Email = "send-not-needed@example.test",
            });

            // The half that keeps the separation honest. A coordinator who may
            // prepare sittings but not mail them must still be able to prepare
            // one; requiring the send permission unconditionally would merge the
            // two back together and nobody would notice, because the refusal test
            // above would still pass.
            GrantEverythingExcept(InternshipManagementSystemPermissions.Assignments.SendEmail);

            var created = await Assignments.CreateAsync(new CreateAssignmentDto
            {
                ExamId = exam,
                CandidateId = candidate.Id,
                ExpiresAt = DateTime.Now.AddDays(7),
                MaxAttempts = 1,
                SendEmail = false,
            });

            created.Recipients.ShouldHaveSingleItem();
        });
    }

    // ------------------------------------------------------ Users.ManageRoles

    [Fact]
    public async Task Changing_which_roles_a_colleague_holds_without_Users_ManageRoles_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var users = GetRequiredService<IUserAppService>();
            var roles = GetRequiredService<IRepository<IdentityRole, Guid>>();

            await roles.InsertAsync(new IdentityRole(Guid.NewGuid(), "Coordinator", Tenant), autoSave: true);
            await roles.InsertAsync(new IdentityRole(Guid.NewGuid(), "Administrator", Tenant), autoSave: true);

            var created = await users.CreateAsync(Draft("escalate", ["Coordinator"]));

            // Editing a colleague and quietly adding yourself — or them — to
            // Administrator. This is the escalation the separate permission
            // exists to stop, and until it was enforced anybody who could edit a
            // colleague could grant any role.
            GrantEverythingExcept(
                InternshipManagementSystemPermissions.IdentityManagement.Users.ManageRoles);

            await RefusedAsync(() => users.UpdateAsync(created.Id, Draft("escalate", ["Administrator"])));

            // And the escalation did not happen anyway. A refusal that still wrote
            // the row would be the worst of both. Asked through the user manager
            // rather than through UserAppService.GetAsync, which is CrudAppService's
            // unoverridden mapping and does not populate Roles at all.
            GrantEverything();

            var userManager = GetRequiredService<IdentityUserManager>();
            var held = await userManager.GetRolesAsync(await userManager.GetByIdAsync(created.Id));

            held.ShouldBe(["Coordinator"]);
        });
    }

    [Fact]
    public async Task Correcting_a_colleagues_details_does_not_need_Users_ManageRoles()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var users = GetRequiredService<IUserAppService>();
            var roles = GetRequiredService<IRepository<IdentityRole, Guid>>();

            await roles.InsertAsync(new IdentityRole(Guid.NewGuid(), "Coordinator", Tenant), autoSave: true);

            var created = await users.CreateAsync(Draft("rename", ["Coordinator"]));

            GrantEverythingExcept(
                InternshipManagementSystemPermissions.IdentityManagement.Users.ManageRoles);

            var edited = Draft("rename", ["Coordinator"]);
            edited.FullName = "اسم مُصحَّح";

            await users.UpdateAsync(created.Id, edited);

            GrantEverything();

            var userManager = GetRequiredService<IdentityUserManager>();
            var user = await userManager.GetByIdAsync(created.Id);

            user.Name.ShouldBe("اسم مُصحَّح");

            // Still held. An edit that dropped the roles it was not allowed to
            // change would pass a test that only checked the name.
            (await userManager.GetRolesAsync(user)).ShouldBe(["Coordinator"]);
        });
    }

    // --------------------------------------------- Review.ViewIntegritySignals

    [Fact]
    public async Task The_results_roster_withholds_integrity_counts_without_the_permission()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var results = GetRequiredService<IResultAppService>();

            var exam = await PublishedExamAsync("integrity");
            var sitting = await StartedSittingAsync(exam, "integrity");

            // Two real observations, so the withheld value and the honest value
            // differ. A candidate with no signals would make redaction and
            // truthfulness indistinguishable — both would read zero.
            await Taking.ReportSignalAsync(sitting.SessionToken, new ReportIntegritySignalDto
            {
                Type = IntegritySignalType.WindowBlur,
            });
            await Taking.ReportSignalAsync(sitting.SessionToken, new ReportIntegritySignalDto
            {
                Type = IntegritySignalType.WindowBlur,
            });

            await Taking.SubmitAsync(sitting.SessionToken);

            // Shown to somebody who may see behavioural data.
            GrantOnly(
                InternshipManagementSystemPermissions.Results.View,
                InternshipManagementSystemPermissions.Review.ViewIntegritySignals);

            var shown = await results.GetListAsync(new ResultListRequestDto { Filter = sitting.Email });

            shown.Items.Single().IntegrityFlagCount.ShouldBe(2);

            // Withheld from somebody who may not. "This candidate was recorded
            // leaving the window twice" is an accusation, and it was reaching
            // everyone who could read a score.
            GrantEverythingExcept(InternshipManagementSystemPermissions.Review.ViewIntegritySignals);

            var withheld = await results.GetListAsync(new ResultListRequestDto { Filter = sitting.Email });

            withheld.Items.Single().IntegrityFlagCount.ShouldBe(0);
        });
    }

    // ------------------------------------------------------------------ helpers

    private static CreateUpdateUserDto Draft(string code, List<string> roles) => new()
    {
        UserName = code,
        Email = code + "@example.test",
        Password = "1q2w3E*",
        FullName = code,
        Roles = roles,
    };
}
