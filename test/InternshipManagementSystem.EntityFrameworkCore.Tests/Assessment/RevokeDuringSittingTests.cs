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
using InternshipManagementSystem.Assessment.Results;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Revoking a link while somebody is sitting on it.
/// <para>
/// The code that revokes describes itself as killing a link "that leaked or went
/// to the wrong person", and it set a flag and stopped there. The session token
/// is signed and self-contained, so nothing on any later call asked whether the
/// link behind it still stood: whoever held the leaked link carried on
/// answering, finished, and submitted.
/// </para>
/// <para>
/// Which is the one case revoking exists for. A leaked link nobody is using is
/// not an emergency. The emergency is somebody using it now, and the emergency
/// stop did not stop them.
/// </para>
/// </summary>
public class RevokeDuringSittingTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000071");

    public RevokeDuringSittingTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_sitting_stops_when_the_link_it_runs_on_is_revoked()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("revoke-a");
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            var question = await _taking.GetQuestionAsync(state.SessionToken!, 0);

            // Answering works, so what follows is the revocation and nothing else.
            await _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "[\"a\"]",
            });

            await _assignments.RevokeLinkAsync(await LinkIdAsync(sent.ExamId));

            var refused = await Should.ThrowAsync<BusinessException>(() =>
                _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = "[\"b\"]",
                }));

            refused.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamLinkRevoked);
        });
    }

    [Fact]
    public async Task It_cannot_be_submitted_either()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("revoke-b");
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await _assignments.RevokeLinkAsync(await LinkIdAsync(sent.ExamId));

            // Refusing the save and allowing the submit would be the same defect
            // wearing a different hat: the paper still lands, and it still counts.
            var refused = await Should.ThrowAsync<BusinessException>(
                () => _taking.SubmitAsync(state.SessionToken!));

            refused.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamLinkRevoked);
        });
    }

    [Fact]
    public async Task What_was_already_written_is_not_thrown_away()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("revoke-c");
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            var question = await _taking.GetQuestionAsync(state.SessionToken!, 0);

            await _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "[\"a\"]",
            });

            await _assignments.RevokeLinkAsync(await LinkIdAsync(sent.ExamId));

            // Stopping a sitting is not the same as destroying it, and the two
            // must not be confused: the person may have been the right person all
            // along, and what they wrote is the evidence of that. Discarding an
            // attempt is a deliberate act somebody performs from the monitor.
            var answers = await GetRequiredService<IRepository<Answer, Guid>>()
                .GetListAsync(a => a.AttemptId == state.AttemptId);

            answers.Count.ShouldBe(1);
            answers.Single().Response.ShouldBe("[\"a\"]");
        });
    }

    [Fact]
    public async Task A_sitting_on_a_link_that_stands_is_left_alone()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("revoke-d");
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            var question = await _taking.GetQuestionAsync(state.SessionToken!, 0);

            // The half that decides whether the check is worth having. A guard
            // that also stops the people it was not written for is not a guard.
            var saved = await _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "[\"a\"]",
            });

            saved.Saved.ShouldBeTrue();

            var outcome = await _taking.SubmitAsync(state.SessionToken!);
            outcome.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task A_candidate_whose_sitting_was_discarded_is_told_in_words()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("discard-a");
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            var question = await _taking.GetQuestionAsync(state.SessionToken!, 0);

            // Supported, not accidental: the monitor lists running sittings and
            // offers to throw one away. The person it happens to is mid-question.
            await GetRequiredService<IAttemptAdminAppService>().DeleteAsync(state.AttemptId);

            var refused = await Should.ThrowAsync<BusinessException>(() =>
                _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = "[\"a\"]",
                }));

            // Measured before it was changed, this was:
            //
            //   There is no such an entity. Entity type:
            //   InternshipManagementSystem.Assessment.Delivery.Attempt, id: 9a9425a4-…
            //
            // A .NET type name and a GUID, shown to somebody sitting an exam.
            refused.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.AttemptNoLongerExists);
        });
    }

    [Fact]
    public async Task The_note_a_coordinator_is_asked_to_write_can_be_read_back()
    {
        await AsTenantAsync(async () =>
        {
            var sent = await SendAsync("ended-a");
            var preview = await _taking.OpenLinkAsync(sent.Token);
            var state = await _taking.StartAsync(preview.SessionToken!);

            await GetRequiredService<IAttemptAdminAppService>().ForceSubmitAsync(
                state.AttemptId, new ForceSubmitDto { Reason = "The room was evacuated." });

            var result = await GetRequiredService<IResultAppService>().GetAsync(state.AttemptId);

            // The monitor asks for this under a label reading "the reason (is
            // recorded)". It was recorded — into a column no screen and no
            // endpoint read back, so on the day it mattered nobody could find it.
            result.Summary.EndedByReason.ShouldBe("The room was evacuated.");

            // And the fact itself, which was never shown either: a paper that was
            // cut short read exactly like one somebody finished.
            result.Summary.EndReason.ShouldBe(nameof(AttemptEndReason.EndedByAdministrator));
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
            FullName = "Sitting when it was revoked",
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

        return new Sent(exam.Id, recipient.Url.Split('/').Last());
    }

    private async Task<Guid> LinkIdAsync(Guid examId)
    {
        var links = await _assignments.GetLinksAsync(examId, new PagedAndSortedResultRequestDto());

        return links.Items.Single().Id;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }

    private sealed record Sent(Guid ExamId, string Token);
}
