using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Getting a link back after the panel that showed it has closed.
/// <para>
/// A token is stored hashed and cannot be recovered — only its first characters
/// survive, which is enough to tell two links apart and not enough to use one.
/// So the panel that appears after sending was the single place a link could be
/// copied, and a coordinator who closed it had lost the address for good. The
/// honest answer is not to keep the credential readable somewhere; it is to be
/// able to issue another.
/// </para>
/// </summary>
public class ReissueLinkTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000051");

    public ReissueLinkTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_reissued_link_works()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("reissue-a");
            var linkId = await LinkIdAsync(sent.ExamId);

            var reissued = await _assignments.ReissueLinkAsync(linkId);

            var preview = await _taking.OpenLinkAsync(reissued.Url.Split('/').Last());

            preview.IsAccessible.ShouldBeTrue(preview.BlockReason);
            preview.CandidateName.ShouldBe(sent.CandidateName);
        });
    }

    [Fact]
    public async Task The_old_link_stops_working()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("reissue-b");
            var linkId = await LinkIdAsync(sent.ExamId);

            await _assignments.ReissueLinkAsync(linkId);

            // Two live addresses for one sitting are two ways to spend the same
            // attempt, and the one that arrives second is the one somebody uses.
            //
            // Reported rather than thrown: the taker's entry screen has to be able
            // to say why a link does not work, and a candidate holding a dead
            // address needs a sentence, not an error page.
            var opened = await _taking.OpenLinkAsync(sent.Token);

            opened.IsAccessible.ShouldBeFalse();
            opened.BlockReason.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamLinkInvalid);
        });
    }

    [Fact]
    public async Task Reissuing_does_not_give_somebody_another_attempt()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("reissue-c");
            var linkId = await LinkIdAsync(sent.ExamId);

            // Sat and submitted, so the one permitted attempt is spent.
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await _taking.SubmitAsync(state.SessionToken!);

            var reissued = await _assignments.ReissueLinkAsync(linkId);
            var reopened = await _taking.OpenLinkAsync(reissued.Url.Split('/').Last());

            // The address is new; the entitlement is not. Reissuing is for
            // somebody who lost the link, not for somebody who wants another go —
            // and the difference has to hold, or a coordinator hands out retakes
            // by accident every time they help.
            reopened.IsAccessible.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Reissuing_a_revoked_link_brings_it_back()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("reissue-d");
            var linkId = await LinkIdAsync(sent.ExamId);

            await _assignments.RevokeLinkAsync(linkId);

            // Revoked links stay revoked unless somebody deliberately reissues
            // one. Reissuing is that deliberate act — the alternative is a
            // coordinator who revokes by mistake and cannot undo it.
            var reissued = await _assignments.ReissueLinkAsync(linkId);
            var preview = await _taking.OpenLinkAsync(reissued.Url.Split('/').Last());

            preview.IsAccessible.ShouldBeTrue(preview.BlockReason);
        });
    }

    [Fact]
    public async Task Reissuing_can_mail_the_new_address()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("reissue-e");
            var linkId = await LinkIdAsync(sent.ExamId);

            // "I never received it" is the commonest reason a link is reissued.
            // Until this existed the only answer was to read a URL down the
            // phone, which is not an answer.
            var reissued = await _assignments.ReissueLinkAsync(linkId, sendEmail: true);

            reissued.EmailSent.ShouldBeTrue(reissued.EmailError);

            var links = await _assignments.GetLinksAsync(
                sent.ExamId, new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());

            // Recorded on the link, not only reported back — the table is where a
            // coordinator looks a day later to see whether it ever went.
            links.Items.Single().EmailSentAt.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Reissuing_without_mailing_leaves_the_send_record_alone()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("reissue-f");
            var linkId = await LinkIdAsync(sent.ExamId);

            var reissued = await _assignments.ReissueLinkAsync(linkId);

            reissued.EmailSent.ShouldBeFalse();

            // Nothing was sent, so nothing claims it was. A coordinator copying
            // the address by hand must not leave a trail saying the candidate was
            // emailed.
            var links = await _assignments.GetLinksAsync(
                sent.ExamId, new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());

            links.Items.Single().EmailSentAt.ShouldBeNull();
        });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record Sent(Guid ExamId, string CandidateName, string Token);

    [Fact]
    public async Task The_characters_support_staff_go_by_follow_the_new_link()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("prefix-a");
            var linkId = await LinkIdAsync(sent.ExamId);

            var reissued = await _assignments.ReissueLinkAsync(linkId);
            var token = reissued.Url.Split('/').Last();

            var links = await _assignments.GetLinksAsync(
                sent.ExamId, new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());

            // The field exists for one job, written on the entity itself: "first
            // characters of the token, for support staff to identify a link
            // without holding it". Reissuing mints a new token and the prefix
            // was left describing the old one — so it stopped doing that job at
            // exactly the moment it is needed, which is after something went
            // wrong and the link had to be replaced.
            //
            // A candidate rings and reads out the first characters of the
            // address they were sent. The coordinator finds nothing, or worse
            // finds a different candidate's link that happens to match.
            links.Items.Single().TokenPrefix.ShouldBe(token[..8]);
        });
    }

    [Fact]
    public async Task A_finished_exam_cannot_be_sat_again_on_the_same_link()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("spent-a");

            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await _taking.SubmitAsync(state.SessionToken!);

            // The third rule on the list every candidate reads before starting:
            // "Once you finish, the exam is submitted and cannot be reopened."
            //
            // It is enforced, and nothing held it. The only test that mentioned
            // exhausted attempts was a browser test that stubs the refusal and
            // checks the wording — so it proves the sentence is readable, not
            // that it is true. A rule with no test under it is one refactor from
            // gone, and this one going means a candidate sits twice and the
            // second sitting overwrites the first.
            var again = await _taking.OpenLinkAsync(sent.Token);

            again.IsAccessible.ShouldBeFalse();
            again.BlockReason.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.ExamLinkAttemptsExhausted);
        });
    }

    [Fact]
    public async Task A_reissued_link_does_not_hand_back_a_spent_attempt()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("spent-b");

            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await _taking.SubmitAsync(state.SessionToken!);

            var linkId = await LinkIdAsync(sent.ExamId);
            var reissued = await _assignments.ReissueLinkAsync(linkId);

            // The half that keeps reissuing honest. Reissuing exists so somebody
            // who lost an address can be given it again — not so a candidate who
            // has already sat can sit a second time. The attempt count is the
            // record of what was used, and a new address does not undo it.
            var opened = await _taking.OpenLinkAsync(reissued.Url.Split('/').Last());

            opened.IsAccessible.ShouldBeFalse();
            opened.BlockReason.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.ExamLinkAttemptsExhausted);
        });
    }

    private async Task<Sent> SendAsync(string code)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
        });

        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.SingleChoice,
            Text = code + " question",
            Score = 1m,
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                ],
            }),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Lost their link",
            Email = code + "@example.test",
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        var recipient = result.Recipients.Single();

        return new Sent(exam.Id, recipient.CandidateName, recipient.Url.Split('/').Last());
    }

    private async Task<Guid> LinkIdAsync(Guid examId)
    {
        var links = await _assignments.GetLinksAsync(
            examId, new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());

        return links.Items.Single().Id;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
