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
/// "Draw ten listening questions from the bank and ten reading."
/// <para>
/// The thing a language centre asks for first, and the thing the product could
/// not do. A part of a paper could only serve questions filed into it by hand —
/// and a shared-bank question cannot be filed into one exam's part, because it
/// belongs to every exam at its level. So the bank and the parts were two
/// features that did not meet: an exam could have parts, or it could draw from
/// the bank, and never both.
/// </para>
/// <para>
/// A part now owns rules of its own, which is where every comparable product
/// keeps this. The rule selects on what a question says about itself — its
/// topic, its difficulty, its type — and those are true in every exam, unlike
/// "which part of which paper", which is true only of one.
/// </para>
/// </summary>
public class SectionBlueprintTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IExamStructureAppService _structure;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000b2");

    public SectionBlueprintTests()
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
    public async Task Ten_listening_and_ten_reading_drawn_from_the_shared_bank()
    {
        await AsTenantAsync(async () =>
        {
            var world = await WorldAsync("blue-a");

            var listening = await SectionAsync(world.ExamId, "Listening", 0);
            var reading = await SectionAsync(world.ExamId, "Reading", 1);

            // Nothing filed into either part. Everything is in the shared bank,
            // where a question for this level belongs.
            await BankAsync(world, world.Listening, 6, "listening");
            await BankAsync(world, world.Reading, 6, "reading");

            await _exams.SetBlueprintAsync(world.ExamId,
            [
                Rule(listening, world.Listening, count: 2, order: 0),
                Rule(reading, world.Reading, count: 3, order: 1),
            ]);

            await _exams.PublishAsync(world.ExamId);

            var paper = await SitAsync(world.ExamId, "blue-a@example.test");

            // Two of the six listening and three of the six reading — not twelve,
            // and not five drawn from whichever twelve came back first.
            paper.Count.ShouldBe(5);
            paper.Count(q => q == "Listening").ShouldBe(2);
            paper.Count(q => q == "Reading").ShouldBe(3);
        });
    }

    [Fact]
    public async Task What_a_part_passed_over_does_not_arrive_anyway()
    {
        await AsTenantAsync(async () =>
        {
            var world = await WorldAsync("blue-b");

            var listening = await SectionAsync(world.ExamId, "Listening", 0);

            await BankAsync(world, world.Listening, 6, "listening");

            await _exams.SetBlueprintAsync(world.ExamId,
            [
                Rule(listening, world.Listening, count: 2, order: 0),
            ]);

            await _exams.PublishAsync(world.ExamId);

            var paper = await SitAsync(world.ExamId, "blue-b@example.test");

            // A part speaks for everything it could have drawn, not only for what
            // it took. Otherwise the four it passed over fall through as unfiled
            // questions and reach the candidate regardless — so a rule that says
            // "two" hands over six, which is not a rule.
            paper.Count.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_rule_aimed_at_another_exams_part_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var mine = await WorldAsync("blue-c");
            var theirs = await WorldAsync("blue-d");

            var elsewhere = await SectionAsync(theirs.ExamId, "Listening", 0);

            // Pointed at a part of a different exam. Left unchecked it draws
            // nothing and reports nothing: the author sees a rule asking for two
            // questions, and a paper that arrives without them.
            var refused = await Should.ThrowAsync<BusinessException>(async () =>
                await _exams.SetBlueprintAsync(mine.ExamId,
                [
                    Rule(elsewhere, mine.Listening, count: 2, order: 0),
                ]));

            refused.Code.ShouldBe(
                InternshipManagementSystemDomainErrorCodes.ExamBlueprintSectionNotInExam);
        });
    }

    [Fact]
    public async Task The_count_beside_a_rule_is_what_that_part_can_actually_draw()
    {
        await AsTenantAsync(async () =>
        {
            var world = await WorldAsync("blue-e");

            var listening = await SectionAsync(world.ExamId, "Listening", 0);

            // Two written straight into the part, and five more on the same topic
            // sitting in the shared bank.
            await FiledAsync(world.ExamId, listening, world.Listening, 2);
            await BankAsync(world, world.Listening, 5, "listening");

            await _exams.SetBlueprintAsync(world.ExamId,
            [
                Rule(listening, world.Listening, count: 4, order: 0),
            ]);

            var rules = await _exams.GetBlueprintAsync(world.ExamId);

            // Two, because somebody put those two questions in this part on
            // purpose and the bank is only what a part falls back to. Counted
            // across the whole bank it would read seven — and the number beside a
            // rule is a promise about what a candidate will be handed.
            rules.Single().AvailableCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task Publishing_is_refused_while_a_part_cannot_be_filled()
    {
        await AsTenantAsync(async () =>
        {
            var world = await WorldAsync("blue-f");

            var listening = await SectionAsync(world.ExamId, "Listening", 0);

            await FiledAsync(world.ExamId, listening, world.Listening, 2);
            await BankAsync(world, world.Listening, 5, "listening");

            await _exams.SetBlueprintAsync(world.ExamId,
            [
                Rule(listening, world.Listening, count: 4, order: 0),
            ]);

            var check = await _exams.CheckPublishAsync(world.ExamId);

            // The half that matters most: the publish check and the builder read
            // the same pool. Two definitions of one rule is how a check approves
            // a paper that then comes up short in front of a candidate.
            check.CanPublish.ShouldBeFalse();
            check.Blockers.ShouldContain(
                InternshipManagementSystemDomainErrorCodes.ExamBlueprintUnsatisfiable);
        });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record World(Guid ExamId, Guid CategoryId, Guid LevelId, Guid Listening, Guid Reading);

    private async Task<World> WorldAsync(string code)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();
        var levels = GetRequiredService<IRepository<Level, Guid>>();
        var topics = GetRequiredService<IRepository<Topic, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var level = await levels.InsertAsync(
            new Level(Guid.NewGuid(), Tenant, code + "-1", code) { CategoryId = category.Id },
            autoSave: true);

        var listening = await topics.InsertAsync(
            new Topic(Guid.NewGuid(), Tenant, code + "-listening", "Listening") { CategoryId = category.Id },
            autoSave: true);

        var reading = await topics.InsertAsync(
            new Topic(Guid.NewGuid(), Tenant, code + "-reading", "Reading") { CategoryId = category.Id },
            autoSave: true);

        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
            LevelId = level.Id,
        });

        return new World(exam.Id, category.Id, level.Id, listening.Id, reading.Id);
    }

    private async Task<Guid> SectionAsync(Guid examId, string name, int order) =>
        (await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
        {
            ExamId = examId,
            Name = name,
            DisplayOrder = order,
        })).Id;

    private static CreateUpdateBlueprintRuleDto Rule(Guid sectionId, Guid topicId, int count, int order) =>
        new()
        {
            ExamSectionId = sectionId,
            TopicId = topicId,
            QuestionCount = count,
            DisplayOrder = order,
        };

    /// <summary>Questions in the shared bank: no exam, so every exam at the level may draw them.</summary>
    private async Task BankAsync(World world, Guid topicId, int count, string code)
    {
        for (var i = 0; i < count; i++)
        {
            await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = null,
                CategoryId = world.CategoryId,
                LevelId = world.LevelId,
                TopicId = topicId,
                Type = QuestionTypes.SingleChoice,
                Text = code + " " + (i + 1),
                Score = 1m,
                DisplayOrder = i,
                Payload = Choice(),
            });
        }
    }

    /// <summary>Questions written straight into one part of one exam.</summary>
    private async Task FiledAsync(Guid examId, Guid sectionId, Guid topicId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = examId,
                ExamSectionId = sectionId,
                TopicId = topicId,
                Type = QuestionTypes.SingleChoice,
                Text = "filed " + (i + 1),
                Score = 1m,
                DisplayOrder = i,
                Payload = Choice(),
            });
        }
    }

    private static string Choice() =>
        PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
            ],
        });

    /// <summary>The part each question on a candidate's paper was served under.</summary>
    private async Task<List<string>> SitAsync(Guid examId, string email)
    {
        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Sat the paper",
            Email = email,
        });

        var sent = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = examId,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        var token = sent.Recipients.Single().Url.Split('/').Last();
        var preview = await _taking.OpenLinkAsync(token);
        var state = await _taking.StartAsync(preview.SessionToken!);

        var parts = new List<string>();

        for (var position = 0; position < state.TotalQuestions; position++)
        {
            var question = await _taking.GetQuestionAsync(state.SessionToken!, position);

            parts.Add(question.Section?.Name ?? "");
        }

        return parts;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
