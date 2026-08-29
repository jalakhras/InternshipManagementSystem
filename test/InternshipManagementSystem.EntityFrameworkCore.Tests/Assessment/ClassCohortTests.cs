using System;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
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
/// A class of students, sitting at a level, with the papers it will sit.
/// <para>
/// The product owner asked for this in his own words: a class for each group of
/// students, under a role or a training level, linked to exam forms. What it
/// buys is the retake guarantee — a form exists so that sitting again means a
/// genuinely different paper, and that only holds if somebody decided in advance
/// which paper the second sitting uses.
/// </para>
/// </summary>
public class ClassCohortTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICandidateAppService _candidates;
    private readonly IExamAppService _exams;
    private readonly IExamStructureAppService _structure;
    private readonly IQuestionAppService _questions;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000031");

    public ClassCohortTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _exams = GetRequiredService<IExamAppService>();
        _structure = GetRequiredService<IExamStructureAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_class_sits_at_a_level_and_says_which_one()
    {
        await AsTenantAsync(async () =>
        {
            var levelId = await LevelAsync("A1");

            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "Evening A1",
                LevelId = levelId,
                StartsOn = new DateTime(2026, 9, 1),
                EndsOn = new DateTime(2026, 12, 15),
            });

            // A cohort that knows its level is part of the curriculum rather than a
            // list of names beside it.
            group.LevelId.ShouldBe(levelId);
            group.LevelName.ShouldBe("A1");

            // And a cohort in time: "Evening A1, autumn" is a different class from
            // "Evening A1, spring", with a different roll and different results.
            group.StartsOn.ShouldBe(new DateTime(2026, 9, 1));
        });
    }

    [Fact]
    public async Task A_class_carries_its_papers_in_the_order_it_sits_them()
    {
        await AsTenantAsync(async () =>
        {
            var (first, second) = await TwoPublishedFormsAsync();

            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "Evening A1",
            });

            var updated = await _candidates.SetGroupFormsAsync(group.Id, new SetGroupFormsDto
            {
                Forms =
                [
                    new GroupFormEntryDto { ExamFormId = first, SittingOn = new DateTime(2026, 12, 1) },
                    new GroupFormEntryDto { ExamFormId = second },
                ],
            });

            // Everyone sits the first; whoever fails sits the second. That is the
            // whole idea, and the order is where it lives.
            updated.Forms.Count.ShouldBe(2);
            updated.Forms[0].ExamFormId.ShouldBe(first);
            updated.Forms[0].Sequence.ShouldBe(0);
            updated.Forms[0].SittingOn.ShouldBe(new DateTime(2026, 12, 1));

            updated.Forms[1].ExamFormId.ShouldBe(second);
            updated.Forms[1].Sequence.ShouldBe(1);

            // Named well enough that a list row reads without a second request.
            updated.Forms[0].FormCode.ShouldBe("F1");
            updated.Forms[0].ExamTitle.ShouldBe("English A1");
        });
    }

    [Fact]
    public async Task The_same_paper_cannot_be_both_the_sitting_and_the_retake()
    {
        await AsTenantAsync(async () =>
        {
            var (first, _) = await TwoPublishedFormsAsync();

            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "Evening A1",
            });

            // A retake identical to the first attempt measures somebody's memory of
            // it rather than what they know, which removes the reason for forms.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _candidates.SetGroupFormsAsync(group.Id, new SetGroupFormsDto
                {
                    Forms =
                    [
                        new GroupFormEntryDto { ExamFormId = first },
                        new GroupFormEntryDto { ExamFormId = first },
                    ],
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.GroupFormRepeated);
        });
    }

    [Fact]
    public async Task An_unpublished_paper_cannot_be_scheduled()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamWithQuestionsAsync();

            var draft = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
            {
                ExamId = exam.Id, Name = "Draft", Code = "D1",
            });

            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "Evening A1",
            });

            // A draft has not been reviewed. Scheduling one is scheduling a paper
            // nobody approved, which is the thing named forms exist to prevent.
            var thrown = await Should.ThrowAsync<BusinessException>(() =>
                _candidates.SetGroupFormsAsync(group.Id, new SetGroupFormsDto
                {
                    Forms = [new GroupFormEntryDto { ExamFormId = draft.Id }],
                }));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.GroupFormNotPublished);
        });
    }

    [Fact]
    public async Task Setting_the_papers_again_replaces_the_order_rather_than_adding_to_it()
    {
        await AsTenantAsync(async () =>
        {
            var (first, second) = await TwoPublishedFormsAsync();

            var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto
            {
                Name = "Evening A1",
            });

            await _candidates.SetGroupFormsAsync(group.Id, new SetGroupFormsDto
            {
                Forms = [new GroupFormEntryDto { ExamFormId = first }, new GroupFormEntryDto { ExamFormId = second }],
            });

            // Reversed. A coordinator correcting the order should get the order
            // they asked for, not both orders at once.
            var updated = await _candidates.SetGroupFormsAsync(group.Id, new SetGroupFormsDto
            {
                Forms = [new GroupFormEntryDto { ExamFormId = second }],
            });

            updated.Forms.Count.ShouldBe(1);
            updated.Forms.Single().ExamFormId.ShouldBe(second);
        });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Guid> LevelAsync(string code)
    {
        var levels = GetRequiredService<IRepository<Level, Guid>>();

        var level = await levels.InsertAsync(new Level(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        return level.Id;
    }

    private async Task<ExamDto> ExamWithQuestionsAsync()
    {
        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = "English A1",
            TimeLimitInMinutes = 30,
            PassingPercentage = 60m,
        });

        await _questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.SingleChoice,
            Text = "Which is correct?",
            Payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                ],
            }),
        });

        return exam;
    }

    private async Task<(Guid First, Guid Second)> TwoPublishedFormsAsync()
    {
        var exam = await ExamWithQuestionsAsync();

        var first = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
        {
            ExamId = exam.Id, Name = "Form 1", Code = "F1",
        });

        var second = await _structure.CreateFormAsync(new CreateUpdateExamFormDto
        {
            ExamId = exam.Id, Name = "Form 2", Code = "F2",
        });

        foreach (var form in new[] { first, second })
        {
            await _structure.GenerateFormAsync(form.Id, new GenerateExamFormDto());
            await _structure.PublishFormAsync(form.Id);
        }

        return (first.Id, second.Id);
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
