using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Authoring exercised through the real services and a real database.
/// <para>
/// The unit tests already cover the validator's rules in isolation. What this adds
/// is that the service actually applies them, that the publish gate refuses on the
/// conditions it claims, and that a question round-trips its payload — the parts
/// that only break once persistence and authorisation are in the path.
/// </para>
/// </summary>
public class QuestionAuthoringTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

    public QuestionAuthoringTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_question_round_trips_its_payload()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Support", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Resistance", IsCorrect = false },
                ],
            });

            var created = await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                Type = QuestionTypes.SingleChoice,
                Text = "Which level is support?",
                Payload = payload,
                Score = 2.5m,
            });

            var fetched = await _questions.GetAsync(created.Id);

            // The payload is free-form JSON, so nothing structural guarantees it
            // survives a round trip — which is exactly why it is worth asserting.
            var spec = PayloadJson.Read<ChoicePayload>(fetched.Payload);
            spec.ShouldNotBeNull();
            spec!.Options.Count.ShouldBe(2);
            spec.Options.Single(o => o.IsCorrect).Text.ShouldBe("Support");

            // Decimal marks survive. The old code truncated to int and lost
            // partial credit on the way.
            fetched.Score.ShouldBe(2.5m);
        });
    }

    [Fact]
    public async Task The_service_refuses_a_payload_no_grader_could_read()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var noCorrectOption = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "A", IsCorrect = false },
                    new OptionPayload { Id = "b", Text = "B", IsCorrect = false },
                ],
            });

            // Saving this would produce a question every candidate fails, and it
            // would look like a hard question rather than a broken one.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _questions.CreateAsync(new CreateUpdateQuestionDto
                {
                    ExamId = exam.Id,
                    Type = QuestionTypes.SingleChoice,
                    Text = "Broken",
                    Payload = noCorrectOption,
                }));

            thrown.Code.ShouldBe("IMS:Question:NoCorrectOption");
        });
    }

    [Fact]
    public async Task An_unknown_type_is_accepted_and_reported_as_human_graded()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            // Extensibility is the point of the payload: a type this build does not
            // know must still save, because the grader resolver routes it to a
            // reviewer rather than scoring it zero.
            var created = await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                Type = "some-future-type",
                Text = "A type from a later build",
                Payload = """{"whatever":true}""",
            });

            created.Type.ShouldBe("some-future-type");

            var advice = await _questions.ValidatePayloadAsync("some-future-type", "{}");
            advice.ShouldContain("IMS:Question:UnknownTypeWillBeManual");
        });
    }

    [Fact]
    public async Task Publishing_is_refused_while_the_exam_has_no_questions()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var check = await _exams.CheckPublishAsync(exam.Id);

            check.CanPublish.ShouldBeFalse();
            check.Blockers.ShouldContain(InternshipManagementSystemDomainErrorCodes.ExamHasNoQuestions);

            await Should.ThrowAsync<BusinessException>(() => _exams.PublishAsync(exam.Id));
        });
    }

    [Fact]
    public async Task Publishing_warns_when_nothing_carries_a_competency()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            await AddValidQuestionAsync(exam.Id);

            var check = await _exams.CheckPublishAsync(exam.Id);

            // Not a blocker: the exam works. But the result will be a bare number
            // nobody can act on, which the author should decide about knowingly.
            check.CanPublish.ShouldBeTrue();
            check.Warnings.ShouldContain("IMS:Exam:NoTopicsAssigned");
        });
    }

    [Fact]
    public async Task Publishing_warns_when_everyone_sits_the_same_paper()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            await AddValidQuestionAsync(exam.Id);

            var check = await _exams.CheckPublishAsync(exam.Id);

            // No blueprint and no form cap means one leaked paper is everyone's
            // paper. Allowed, but worth saying out loud.
            check.Warnings.ShouldContain("IMS:Exam:EveryoneGetsTheSameForm");
        });
    }

    [Fact]
    public async Task Publishing_is_refused_when_the_form_asks_for_more_than_the_bank_holds()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            await AddValidQuestionAsync(exam.Id);

            // One question in the bank, ten wanted per form. Left alone this
            // silently shortens every candidate's paper.
            await _exams.UpdateAsync(exam.Id, new CreateUpdateExamDto
            {
                Title = exam.Title,
                TimeLimitInMinutes = exam.TimeLimitInMinutes,
                PassingPercentage = exam.PassingPercentage,
                QuestionsPerForm = 10,
                Mode = exam.Mode,
            });

            var check = await _exams.CheckPublishAsync(exam.Id);

            check.CanPublish.ShouldBeFalse();
            check.Blockers.ShouldContain(InternshipManagementSystemDomainErrorCodes.ExamFormLargerThanBank);
        });
    }

    [Fact]
    public async Task A_published_exam_reports_itself_as_open()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();
            await AddValidQuestionAsync(exam.Id);

            var published = await _exams.PublishAsync(exam.Id);
            published.Status.ShouldBe(ExamStatus.Published);

            var entity = await GetRequiredService<IRepository<Exam, Guid>>().GetAsync(exam.Id);
            entity.IsOpenAt(DateTime.UtcNow).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Every_shipped_question_type_has_a_descriptor()
    {
        await AsTenantAsync(async () =>
        {
            var types = await _questions.GetTypesAsync();

            // The picker is built from this list, so a type the graders know but
            // the catalogue does not would be unreachable in the UI.
            types.Select(t => t.Type).ShouldContain(QuestionTypes.SingleChoice);
            types.Select(t => t.Type).ShouldContain(QuestionTypes.AudioResponse);
            types.Count.ShouldBe(13);

            // The human-marked types must not claim otherwise: the difference
            // decides whether a submission lands in the review queue.
            types.Single(t => t.Type == QuestionTypes.Text).IsAutoGraded.ShouldBeFalse();
            types.Single(t => t.Type == QuestionTypes.SingleChoice).IsAutoGraded.ShouldBeTrue();
        });
    }

    // ------------------------------------------------------------------ helpers

    [Fact]
    public async Task A_scale_question_cannot_carry_marks()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _questions.CreateAsync(new CreateUpdateQuestionDto
                {
                    ExamId = exam.Id,
                    Type = QuestionTypes.Scale,
                    Text = "How confident do you feel about this topic?",
                    Score = 2m,
                    Payload = PayloadJson.Write(new ScalePayload { Min = 1, Max = 5 }),
                }));

            // ScaleGrader always awards nothing, deliberately — it is a survey
            // item with no right answer. But the attempt's maximum is the sum of
            // every question on the paper, so two marks here is two marks off
            // everybody, for a question nobody can get wrong.
            thrown.Code.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.QuestionScaleCarriesNoMarks);
        });
    }

    [Fact]
    public async Task A_scale_question_with_no_marks_is_accepted()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await CreateExamAsync();

            var question = await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                Type = QuestionTypes.Scale,
                Text = "How confident do you feel about this topic?",
                Score = 0m,
                Payload = PayloadJson.Write(new ScalePayload { Min = 1, Max = 5 }),
            });

            // The type is not being banned — asking how somebody feels is a real
            // thing to put on a paper. It just cannot be marked out of anything.
            question.Score.ShouldBe(0m);
        });
    }

    private async Task<ExamDto> CreateExamAsync() =>
        await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = "Technical Analysis",
            TimeLimitInMinutes = 45,
            PassingPercentage = 60m,
        });

    private async Task AddValidQuestionAsync(Guid examId) =>
        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = examId,
            Type = QuestionTypes.SingleChoice,
            Text = "Which is a support level?",
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "1.0820", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "1.0980", IsCorrect = false },
                ],
            }),
        });

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
