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
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Moving a deadline, for somebody who missed it.
/// <para>
/// Reissuing gives a lost link a new address and deliberately leaves the
/// deadline where it was — they are different decisions. But until there was a
/// way to make the second one, a coordinator helping somebody who missed Friday
/// had only the first: they reissued, read out a fresh address, and it was
/// already dead. The token was new and the deadline was not, and nothing on the
/// screen said so.
/// </para>
/// </summary>
public class ExtendLinkTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000052");

    public ExtendLinkTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_expired_link_works_again_once_the_deadline_moves()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("extend-a");

            // Aged rather than created expired: CreateAsync refuses a deadline
            // that has already passed, and rightly — nobody means to send an exam
            // that is over. What has to be reproduced is Friday arriving, not a
            // coordinator typing last week.
            await ExpireAsync(sent.ExamId);

            var blocked = await _taking.OpenLinkAsync(sent.Token);
            blocked.IsAccessible.ShouldBeFalse();

            await _assignments.ExtendLinkAsync(await LinkIdAsync(sent.ExamId), DateTime.Now.AddDays(3));

            // The same address, still in the candidate's inbox. Extending is for
            // the person who already has the link and ran out of time — sending
            // them a new one to fix a date would be answering a different problem.
            var opened = await _taking.OpenLinkAsync(sent.Token);

            opened.IsAccessible.ShouldBeTrue(opened.BlockReason);
        });
    }

    [Fact]
    public async Task A_deadline_cannot_be_moved_into_the_past()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("extend-b");

            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _assignments.ExtendLinkAsync(
                    await LinkIdAsync(sent.ExamId), DateTime.Now.AddDays(-1)));

            // Refused rather than accepted-and-ignored. A date already gone is
            // not an extension, and a coordinator who mistypes a year deserves to
            // be told rather than to watch the link keep failing.
            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamLinkExpiryInPast);
        });
    }

    [Fact]
    public async Task A_deadline_cannot_be_pulled_back()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("extend-c", expiresAt: DateTime.Now.AddDays(10));

            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _assignments.ExtendLinkAsync(
                    await LinkIdAsync(sent.ExamId), DateTime.Now.AddDays(2)));

            // Shortening a window ends a sitting under whoever is part way
            // through it, with no warning and no way to ask for it back. Closing
            // an exam early is what revoking is for, and revoking says so to the
            // person holding the link.
            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamLinkExpiryMovedBack);
        });
    }

    [Fact]
    public async Task Extending_does_not_hand_back_an_attempt()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("extend-d");
            var linkId = await LinkIdAsync(sent.ExamId);

            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            await _taking.SubmitAsync(state.SessionToken!);

            await _assignments.ExtendLinkAsync(linkId, DateTime.Now.AddDays(30));

            // More time is not another go. Somebody who sat the exam and finished
            // it has spent the one attempt they were given, and a coordinator
            // fixing a date must not be handing out retakes without knowing.
            var reopened = await _taking.OpenLinkAsync(sent.Token);

            reopened.IsAccessible.ShouldBeFalse();
        });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record Sent(Guid ExamId, string Token);

    private async Task<Sent> SendAsync(string code, DateTime? expiresAt = null)
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
            FullName = "Missed the deadline",
            Email = code + "@example.test",
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            ExpiresAt = expiresAt ?? DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        return new Sent(exam.Id, result.Recipients.Single().Url.Split('/').Last());
    }

    /// <summary>Moves a link's deadline into the past, the way time does.</summary>
    private async Task ExpireAsync(Guid examId)
    {
        var links = GetRequiredService<IRepository<ExamLink, Guid>>();
        var link = await links.GetAsync(await LinkIdAsync(examId));

        link.ExpiresAt = DateTime.Now.AddMinutes(-5);

        await links.UpdateAsync(link, autoSave: true);
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
