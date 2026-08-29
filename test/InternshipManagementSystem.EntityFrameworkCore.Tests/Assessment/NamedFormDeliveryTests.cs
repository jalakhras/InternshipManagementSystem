using System;
using System.Collections.Generic;
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
/// Whether naming a paper actually changes what a candidate is asked.
/// <para>
/// Written after a review found that it did not. Forms could be authored,
/// ordered, published and reported on, and nothing read one when an attempt
/// started: every sitting drew a fresh random paper from the bank. A coordinator
/// could nominate Form 2 for the retake and every student would silently receive
/// a draw, with no error and nothing in the result to say so.
/// </para>
/// <para>
/// So these cross from authoring into delivery on purpose. That crossing is the
/// only place the defect was visible: both halves were individually correct and
/// individually tested.
/// </para>
/// </summary>
public class NamedFormDeliveryTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IExamStructureAppService _structure;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000011");

    public NamedFormDeliveryTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _structure = GetRequiredService<IExamStructureAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_sitting_sent_on_a_named_form_serves_that_form()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-a", 6);
            var chosen = exam.Bank.Take(3).Select(q => q.Id).ToList();

            var form = await PublishedFormAsync(exam.Id, "Form 1", "F1", chosen);
            var token = await SendAsync(exam.Id, "sara@example.test", form.Id);

            var paper = await PaperAsync(token);

            // Not "three questions": these three, in this order. A count would have
            // passed against the old behaviour too, because a draw from a
            // six-question bank can perfectly well return three.
            paper.Select(q => q.Id).ShouldBe(chosen);
        });
    }

    [Fact]
    public async Task Two_candidates_on_one_form_answer_the_same_paper()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-b", 8);
            var chosen = exam.Bank.Take(4).Select(q => q.Id).ToList();

            var form = await PublishedFormAsync(exam.Id, "Form 1", "F1", chosen);

            var first = await PaperAsync(await SendAsync(exam.Id, "one@example.test", form.Id));
            var second = await PaperAsync(await SendAsync(exam.Id, "two@example.test", form.Id));

            // The entire reason a named form exists. Two scores mean the same thing
            // only if the papers behind them were the same paper.
            first.Select(q => q.Id).ShouldBe(second.Select(q => q.Id).ToList());
        });
    }

    [Fact]
    public async Task A_sitting_with_no_form_still_draws_a_paper()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-c", 5);

            var paper = await PaperAsync(await SendAsync(exam.Id, "draw@example.test", formId: null));

            // Practice and self-assessment still want a fresh draw each time, so
            // naming a form has to stay a choice rather than become a requirement.
            paper.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public async Task Serving_a_form_counts_against_its_exposure()
    {
        Guid formId = default;

        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-d", 4);
            var chosen = exam.Bank.Take(2).Select(q => q.Id).ToList();

            var form = await PublishedFormAsync(exam.Id, "Form 1", "F1", chosen);
            formId = form.Id;

            await PaperAsync(await SendAsync(exam.Id, "seen@example.test", form.Id));
            await PaperAsync(await SendAsync(exam.Id, "also@example.test", form.Id));
        });

        // Read in a second unit of work, because that is what a coordinator's next
        // page load is. The counter is incremented by a set-based update — a whole
        // cohort sits one paper, and read-modify-write on a shared row is a queue
        // most of them lose — and such an update deliberately leaves the change
        // tracker alone. Asserting inside the same unit of work would be asserting
        // about the tracker rather than about the database.
        await AsTenantAsync(async () =>
        {
            // Exposure accrues per paper as well as per question: a form in front of
            // enough people has circulated whatever its questions' individual counts
            // say, and this is the number a coordinator retires a paper on.
            (await _structure.GetFormAsync(formId)).TimesUsed.ShouldBe(2);
        });
    }

    [Fact]
    public async Task An_attempt_records_which_paper_it_was_served()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-e", 4);

            var form = await PublishedFormAsync(
                exam.Id, "Form 1", "F1", exam.Bank.Take(2).Select(q => q.Id).ToList());

            var state = await StartAsync(await SendAsync(exam.Id, "record@example.test", form.Id));

            var attempts = GetRequiredService<IRepository<Attempt, Guid>>();
            var attempt = await attempts.GetAsync(state.AttemptId);

            // Written down at the time rather than inferred later, because a form can
            // be retired after somebody sat it and a result only means what it meant
            // if the paper behind it is still known.
            attempt.ExamFormId.ShouldBe(form.Id);
        });
    }

    [Fact]
    public async Task A_matching_question_on_a_named_form_does_not_arrive_in_its_authored_order()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-key", 2);

            var matching = await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                Type = QuestionTypes.Matching,
                Text = "Match each word to its meaning",
                Score = 4m,
                Payload = PayloadJson.Write(new MatchingPayload
                {
                    Pairs =
                    [
                        new MatchingPair { LeftId = "l1", LeftText = "cat", RightId = "r1", RightText = "قطة" },
                        new MatchingPair { LeftId = "l2", LeftText = "dog", RightId = "r2", RightText = "كلب" },
                        new MatchingPair { LeftId = "l3", LeftText = "bird", RightId = "r3", RightText = "طائر" },
                        new MatchingPair { LeftId = "l4", LeftText = "fish", RightId = "r4", RightText = "سمكة" },
                    ],
                }),
            });

            var form = await PublishedFormAsync(exam.Id, "Form 1", "F1", [matching.Id]);
            var state = await StartAsync(await SendAsync(exam.Id, "key@example.test", form.Id));

            var rows = GetRequiredService<IRepository<AttemptQuestion, Guid>>();
            var served = (await rows.GetQueryableAsync()).Single(q => q.AttemptId == state.AttemptId);

            // The right column has to carry a recorded order or the projector emits
            // it as authored, and left[i] then pairs with right[i] in the JSON the
            // candidate is handed. That is the answer key. The drawn path always got
            // this right; the named-form path was written separately and did not,
            // which is why the check is here rather than on the projector.
            served.OptionOrder.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task A_rotating_sitting_gives_a_retake_a_different_paper()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-rotate", 6);

            var first = await PublishedFormAsync(
                exam.Id, "Form 1", "R-F1", exam.Bank.Take(3).Select(q => q.Id).ToList());

            var second = await PublishedFormAsync(
                exam.Id, "Form 2", "R-F2", exam.Bank.Skip(3).Take(3).Select(q => q.Id).ToList());

            var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Twice",
                Email = "twice@example.test",
            });

            // Two sittings for one person, both set to rotate rather than naming a
            // paper. This is what a resit looks like.
            var firstPaper = await PaperAsync(await SendRotatingAsync(exam.Id, candidate.Id));
            var secondPaper = await PaperAsync(await SendRotatingAsync(exam.Id, candidate.Id));

            var firstIds = firstPaper.Select(q => q.Id).ToList();
            var secondIds = secondPaper.Select(q => q.Id).ToList();

            // The whole reason named forms exist, made automatic. A retake on the
            // same paper measures what somebody remembers of the first attempt, and
            // a coordinator should not have to hold that in their head at the
            // moment they press send.
            firstIds.ShouldNotBe(secondIds);
            firstIds.Intersect(secondIds).ShouldBeEmpty();

            var forms = new[] { first.Id, second.Id };
            forms.ShouldContain(f => f == first.Id);
        });
    }

    [Fact]
    public async Task Rotation_wraps_rather_than_refusing_a_third_sitting()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-wrap", 4);

            await PublishedFormAsync(exam.Id, "Form 1", "W-F1", exam.Bank.Take(2).Select(q => q.Id).ToList());
            await PublishedFormAsync(exam.Id, "Form 2", "W-F2", exam.Bank.Skip(2).Take(2).Select(q => q.Id).ToList());

            var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Thrice",
                Email = "thrice@example.test",
            });

            var first = await PaperAsync(await SendRotatingAsync(exam.Id, candidate.Id));
            await PaperAsync(await SendRotatingAsync(exam.Id, candidate.Id));
            var third = await PaperAsync(await SendRotatingAsync(exam.Id, candidate.Id));

            // Two papers and three sittings. Wrapping is honest; the alternative is
            // refusing to let somebody sit an exam because the authoring ran out of
            // forms, which is not their problem.
            third.Select(q => q.Id).ShouldBe(first.Select(q => q.Id).ToList());
        });
    }

    [Fact]
    public async Task A_draft_form_cannot_be_sent()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithBankAsync("delivery-f", 4);

            var draft = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id,
                Name = "Not reviewed yet",
                Code = "D1",
            });

            await _structure.SetFormQuestionsAsync(draft.Id, new SetExamFormQuestionsDto
            {
                QuestionIds = exam.Bank.Take(2).Select(q => q.Id).ToList(),
            });

            // Refused when the sitting is created, not when somebody sits down. Once
            // the links are out they cannot be taken back.
            var thrown = await Should.ThrowAsync<BusinessException>(
                async () => await SendAsync(exam.Id, "early@example.test", draft.Id));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.AssignmentFormNotPublished);
        });
    }

    [Fact]
    public async Task A_form_belonging_to_another_exam_cannot_be_sent()
    {
        await AsTenantAsync(async () =>
        {
            var mine = await ExamWithBankAsync("delivery-g", 4);
            var theirs = await ExamWithBankAsync("delivery-h", 4);

            var wrong = await PublishedFormAsync(
                theirs.Id, "Their form", "T1", theirs.Bank.Take(2).Select(q => q.Id).ToList());

            var thrown = await Should.ThrowAsync<BusinessException>(
                async () => await SendAsync(mine.Id, "mixed@example.test", wrong.Id));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.AssignmentFormNotAvailable);
        });
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>An exam at its own level, published, with a bank of questions behind it.</summary>
    private async Task<ExamWithBank> ExamWithBankAsync(string code, int bankSize)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();
        var levels = GetRequiredService<IRepository<Level, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var level = await levels.InsertAsync(
            new Level(Guid.NewGuid(), Tenant, code + "-1", code) { CategoryId = category.Id },
            autoSave: true);

        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
            LevelId = level.Id,
        });

        var bank = new List<QuestionDto>();

        for (var i = 0; i < bankSize; i++)
        {
            bank.Add(await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                Type = QuestionTypes.SingleChoice,
                Text = code + " question " + (i + 1),
                Score = 1m,
                Payload = PayloadJson.Write(new ChoicePayload
                {
                    Options =
                    [
                        new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                        new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                    ],
                }),
            }));
        }

        await _exams.PublishAsync(exam.Id);

        return new ExamWithBank(exam.Id, bank);
    }

    private async Task<ExamFormDto> PublishedFormAsync(
        Guid examId,
        string name,
        string code,
        List<Guid> questionIds)
    {
        var form = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
        {
            ExamId = examId,
            Name = name,
            Code = code,
        });

        await _structure.SetFormQuestionsAsync(form.Id, new SetExamFormQuestionsDto
        {
            QuestionIds = questionIds,
        });

        return await _structure.PublishFormAsync(form.Id);
    }

    /// <summary>Creates a candidate, sends them the sitting, and returns their link token.</summary>
    private async Task<string> SendAsync(Guid examId, string email, Guid? formId)
    {
        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = email.Split('@')[0],
            Email = email,
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = examId,
            ExamFormId = formId,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,

            // No mail server in a test host, and the link comes back in the result
            // either way.
            SendEmail = false,
        });

        return result.Recipients.Single().Url.Split('/').Last();
    }

    /// <summary>Sends a sitting set to rotate, and returns the link token.</summary>
    private async Task<string> SendRotatingAsync(Guid examId, Guid candidateId)
    {
        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = examId,
            RotateForms = true,
            CandidateId = candidateId,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        return result.Recipients.Single().Url.Split('/').Last();
    }

    private async Task<AttemptStateDto> StartAsync(string linkToken)
    {
        var preview = await _taking.OpenLinkAsync(linkToken);

        preview.IsAccessible.ShouldBeTrue(preview.BlockReason);

        return await _taking.StartAsync(preview.SessionToken!);
    }

    /// <summary>The paper as the candidate sees it, in the order they see it.</summary>
    private async Task<List<TakerQuestionDto>> PaperAsync(string linkToken)
    {
        var preview = await _taking.OpenLinkAsync(linkToken);

        preview.IsAccessible.ShouldBeTrue(preview.BlockReason);

        // The start hands back a new credential, because the one from the preview
        // names no attempt. Using the old one is what the client did, and every
        // question after the start came back "no such attempt".
        var state = await _taking.StartAsync(preview.SessionToken!);
        var session = state.SessionToken!;

        var paper = new List<TakerQuestionDto>();

        for (var position = 0; position < state.TotalQuestions; position++)
        {
            paper.Add(await _taking.GetQuestionAsync(session, position));
        }

        return paper;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }

    private sealed record ExamWithBank(Guid Id, List<QuestionDto> Bank);
}
