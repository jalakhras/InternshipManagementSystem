using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People.Dtos;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// The candidate's path, which must work while holding nothing at all.
/// <para>
/// A candidate has no account and never gets one. A link is exchanged for a token
/// scoped to a single attempt, and every method on <c>ExamTakingAppService</c>
/// authorises against that token rather than against the staff permission system —
/// which is why the service is the single entry in
/// <c>AuthorizationCoverageTests.DeliberatelyAnonymous</c>.
/// </para>
/// <para>
/// That list is a promise, and nothing checked it. Adding an <c>[Authorize]</c>
/// anywhere on this service — or on a base class it inherits — would stop every
/// candidate in the product from sitting an exam, while the whole backend suite
/// stayed green, because under <c>AddAlwaysAllowAuthorization</c> the attribute
/// would never run.
/// </para>
/// <para>
/// This is the other direction of the same question: not "is the guard enforced"
/// but "is the absence of a guard preserved". The candidate half of each test runs
/// as nobody — no account, no claims, no permissions — because a signed-in
/// principal with no permissions is not enough: a bare <c>[Authorize]</c> resolves
/// to <c>RequireAuthenticatedUser()</c>, which a signed-in staff principal
/// satisfies. Written that way these tests stayed green when <c>[Authorize]</c> was
/// added to <c>ExamTakingAppService</c> in a scratchpad copy, which is exactly the
/// disease this file is meant to treat.
/// </para>
/// </summary>
public class AnonymousByDesignTests : AssessmentPermissionTestBase
{
    [Fact]
    public async Task A_candidate_holding_no_permission_can_open_sit_and_submit()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("anonymous");

            // Everything from here is the candidate, who is nobody at all.
            var sitting = await StartedSittingAsync(exam, "anonymous");

            SignOutCompletely();

            var question = await Taking.GetQuestionAsync(sitting.SessionToken, 0);

            await Taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "لأن الحجم جفّ عند القمة.",
                TimeSpentSeconds = 200,
                KeystrokeCount = 60,
                BackspaceCount = 5,
            });

            var submitted = await Taking.SubmitAsync(sitting.SessionToken);

            submitted.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Opening_the_link_itself_needs_no_permission()
    {
        await AsTenantAsync(async () =>
        {
            GrantEverything();

            var exam = await PublishedExamAsync("anonymous-open");

            var candidate = await Candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "No account",
                Email = "anonymous-open@example.test",
            });

            var sent = await Assignments.CreateAsync(new CreateAssignmentDto
            {
                ExamId = exam,
                CandidateId = candidate.Id,
                ExpiresAt = System.DateTime.Now.AddDays(7),
                MaxAttempts = 1,
                SendEmail = false,
            });

            var token = sent.Recipients[0].Url.Split('/')[^1];

            SignOutCompletely();

            var preview = await Taking.OpenLinkAsync(token);

            // The exam title reaches somebody with no account, which is the whole
            // point of the link.
            preview.ExamTitle.ShouldBe("anonymous-open");
        });
    }
}
