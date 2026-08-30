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
using InternshipManagementSystem.Assessment.Results;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Whether laying an exam out in parts reaches the person sitting it.
/// <para>
/// It did not. Sections were authorable, orderable, countable and complete on
/// the authoring screen, and the word never appeared once in the delivery
/// namespace — <c>AttemptQuestion</c> had no section at all. A teacher who split
/// a placement test into Listening, Reading and Grammar got a flat shuffled
/// paper and a single percentage, and nothing anywhere said so.
/// </para>
/// <para>
/// These cross from authoring into delivery on purpose. That crossing is the
/// only place the defect was visible: the authoring half was correct and tested,
/// the delivery half was correct and tested, and between them the feature did
/// not exist.
/// </para>
/// </summary>
public class SectionDeliveryTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IExamStructureAppService _structure;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IResultAppService _results;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000021");

    public SectionDeliveryTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _structure = GetRequiredService<IExamStructureAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _results = GetRequiredService<IResultAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_sectioned_exam_delivers_its_parts_one_after_another()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-order", ("Listening", 3, null), ("Grammar", 3, null));

            var paper = await PaperAsync(await SendAsync(exam.Id, "order@example.test"));

            // Contiguous, and in the authored order. Not "three of each": the
            // count passed against the old flat draw too, because a shuffle of six
            // questions still returns six.
            paper.Select(q => q.Section?.Name).ShouldBe(
                new List<string?> { "Listening", "Listening", "Listening", "Grammar", "Grammar", "Grammar" });
        });
    }

    [Fact]
    public async Task The_candidate_is_told_which_part_they_are_in()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-name", ("Listening", 2, "You will hear each clip once."));

            var paper = await PaperAsync(await SendAsync(exam.Id, "named@example.test"));

            // The name is the point. A candidate who does not know they have moved
            // into listening cannot tell a coordinator afterwards which part went
            // badly, and the heading is the only thing that says it.
            paper[0].Section.ShouldNotBeNull();
            paper[0].Section!.Name.ShouldBe("Listening");
            paper[0].Section!.QuestionCount.ShouldBe(2);
            paper[0].Section!.Position.ShouldBe(1);
            paper[1].Section!.Position.ShouldBe(2);
        });
    }

    [Fact]
    public async Task A_sections_instructions_arrive_where_the_section_begins_and_nowhere_else()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync(
                "sec-brief",
                ("Listening", 3, "You will hear each recording once. You cannot go back."),
                ("Grammar", 2, "Answer every question."));

            var paper = await PaperAsync(await SendAsync(exam.Id, "brief@example.test"));

            // They are written to be read before a part starts — how many
            // questions, whether the audio plays once, whether they can go back.
            paper[0].Section!.IsFirstQuestion.ShouldBeTrue();
            paper[0].Section!.Instructions.ShouldBe("You will hear each recording once. You cannot go back.");

            // And nowhere else. Repeating them over question three is something a
            // candidate has to read past under time pressure.
            paper[1].Section!.Instructions.ShouldBeNull();
            paper[2].Section!.Instructions.ShouldBeNull();

            // The next part announces its own, at its own first question.
            paper[3].Section!.IsFirstQuestion.ShouldBeTrue();
            paper[3].Section!.Instructions.ShouldBe("Answer every question.");
        });
    }

    [Fact]
    public async Task A_section_serves_the_number_of_questions_it_asks_for()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-draw", ("Listening", 8, null), ("Grammar", 8, null));

            // Eight authored in each, three drawn from the first and two from the
            // second — the ordinary shape of a bank bigger than the paper.
            await SetDrawAsync(exam.Id, "Listening", 3);
            await SetDrawAsync(exam.Id, "Grammar", 2);

            var paper = await PaperAsync(await SendAsync(exam.Id, "draw@example.test"));

            paper.Count.ShouldBe(5);
            paper.Count(q => q.Section?.Name == "Listening").ShouldBe(3);
            paper.Count(q => q.Section?.Name == "Grammar").ShouldBe(2);

            // And the count the candidate is shown is the paper's, not the bank's.
            // "Question 1 of 8" over a three-question part is a candidate budgeting
            // their time against a number that is not true.
            paper.First(q => q.Section?.Name == "Listening").Section!.QuestionCount.ShouldBe(3);
        });
    }

    [Fact]
    public async Task The_screen_before_the_clock_starts_counts_the_paper_and_not_the_bank()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-count", ("Listening", 8, null), ("Grammar", 8, null));

            await SetDrawAsync(exam.Id, "Listening", 3);
            await SetDrawAsync(exam.Id, "Grammar", 2);

            var preview = await _taking.OpenLinkAsync(await SendAsync(exam.Id, "count@example.test"));

            // Sixteen authored, five served. Showing sixteen would tell somebody
            // deciding whether they have time tonight to budget for three times
            // the paper they are about to get — and the number only became wrong
            // when the section counts started being honoured.
            preview.QuestionCount.ShouldBe(5);
        });
    }

    [Fact]
    public async Task A_section_with_no_questions_never_reaches_the_candidate()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-empty", ("Listening", 3, "Listen."));

            // Created and never filled: the ordinary state of an exam halfway
            // through being authored.
            await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
            {
                ExamId = exam.Id,
                Name = "Writing",
                Instructions = "Write 200 words.",
                DisplayOrder = 1,
            });

            var paper = await PaperAsync(await SendAsync(exam.Id, "empty@example.test"));

            // No heading for a part the candidate is asked nothing about, and no
            // instructions telling them to write 200 words with nowhere to write.
            paper.ShouldAllBe(q => q.Section!.Name == "Listening");
        });
    }

    [Fact]
    public async Task The_result_breaks_down_by_section_as_well_as_by_topic()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-result", ("Listening", 2, null), ("Grammar", 2, null));

            var token = await SendAsync(exam.Id, "result@example.test");
            var preview = await _taking.OpenLinkAsync(token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            var session = state.SessionToken!;

            // Listening answered right, grammar answered wrong. A single
            // percentage cannot tell those two apart, and telling them apart is
            // the entire placement-test story.
            for (var position = 0; position < state.TotalQuestions; position++)
            {
                var question = await _taking.GetQuestionAsync(session, position);

                await _taking.SaveAnswerAsync(session, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = PayloadJson.Write(
                        new[] { question.Section!.Name == "Listening" ? "a" : "b" }),
                });
            }

            var result = await _taking.SubmitAsync(session);

            result.SectionBreakdown.Count.ShouldBe(2);

            // In the exam's own order, so it reads back against the paper the
            // candidate remembers sitting.
            result.SectionBreakdown.Select(s => s.SectionName)
                  .ShouldBe(new List<string> { "Listening", "Grammar" });

            result.SectionBreakdown[0].Percentage.ShouldBe(100m);
            result.SectionBreakdown[1].Percentage.ShouldBe(0m);

            // Alongside the topic breakdown rather than instead of it. Both survive.
            result.TopicBreakdown.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public async Task The_coordinators_result_breaks_down_by_section_too()
    {
        Guid attemptId = default;

        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-coord", ("Listening", 2, null), ("Grammar", 2, null));

            var token = await SendAsync(exam.Id, "coord@example.test");
            var preview = await _taking.OpenLinkAsync(token);
            var state = await _taking.StartAsync(preview.SessionToken!);
            var session = state.SessionToken!;

            attemptId = state.AttemptId;

            for (var position = 0; position < state.TotalQuestions; position++)
            {
                var question = await _taking.GetQuestionAsync(session, position);

                await _taking.SaveAnswerAsync(session, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = PayloadJson.Write(new[] { "a" }),
                });
            }

            await _taking.SubmitAsync(session);
        });

        await AsTenantAsync(async () =>
        {
            var detail = await _results.GetAsync(attemptId);

            // The placement decision is made on this screen. A coordinator reading
            // "64%" cannot place a student; "listening 40, grammar 90" places them.
            detail.BySection.Select(s => s.SectionName)
                  .ShouldBe(new List<string> { "Listening", "Grammar" });

            detail.BySection.Sum(s => s.QuestionCount).ShouldBe(detail.Answers.Count);
            detail.ByTopic.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public async Task An_exam_with_no_sections_delivers_exactly_as_it_did()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("sec-none");
            await BankAsync(exam.Id, "sec-none", null, 4);
            await _exams.PublishAsync(exam.Id);

            var paper = await PaperAsync(await SendAsync(exam.Id, "flat@example.test"));

            // Most exams are one undivided paper, and nothing here may change them.
            paper.Count.ShouldBe(4);
            paper.ShouldAllBe(q => q.Section == null);
        });
    }

    [Fact]
    public async Task A_paper_records_its_sections_so_a_later_edit_cannot_rewrite_an_old_result()
    {
        Guid attemptId = default;
        Guid listeningId = default;

        await AsTenantAsync(async () =>
        {
            var exam = await SectionedExamAsync("sec-frozen", ("Listening", 2, null));

            listeningId = (await _structure.GetSectionsAsync(exam.Id)).Single(s => s.Name == "Listening").Id;

            var preview = await _taking.OpenLinkAsync(await SendAsync(exam.Id, "frozen@example.test"));
            attemptId = (await _taking.StartAsync(preview.SessionToken!)).AttemptId;
        });

        await AsTenantAsync(async () =>
        {
            // Deleting a section releases its questions back to the exam. That is
            // right for authoring, and it must not reach backwards into a paper
            // somebody has already been served.
            await _structure.DeleteSectionAsync(listeningId);
        });

        await AsTenantAsync(async () =>
        {
            var rows = GetRequiredService<IRepository<AttemptQuestion, Guid>>();

            var served = (await rows.GetQueryableAsync())
                .Where(q => q.AttemptId == attemptId)
                .ToList();

            // The paper still knows which part each question was served under. Read
            // back off the question instead, this attempt would now claim it had no
            // sections at all — a result quietly rewritten by next term's authoring.
            served.ShouldAllBe(q => q.ExamSectionId == listeningId);
        });
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// A published exam with the named sections, each holding the given number of
    /// single-choice questions whose right answer is "a".
    /// </summary>
    private async Task<ExamDto> SectionedExamAsync(
        string code,
        params (string Name, int Questions, string? Instructions)[] sections)
    {
        var exam = await ExamAsync(code);

        var order = 0;

        foreach (var spec in sections)
        {
            var section = await _structure.CreateSectionAsync(new CreateUpdateExamSectionDto
            {
                ExamId = exam.Id,
                Name = spec.Name,
                Instructions = spec.Instructions,
                DisplayOrder = order++,
            });

            await BankAsync(exam.Id, code + "-" + spec.Name, section.Id, spec.Questions);
        }

        await _exams.PublishAsync(exam.Id);

        return exam;
    }

    private async Task<ExamDto> ExamAsync(string code)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();
        var levels = GetRequiredService<IRepository<Level, Guid>>();
        var topics = GetRequiredService<IRepository<Topic, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var level = await levels.InsertAsync(
            new Level(Guid.NewGuid(), Tenant, code + "-1", code) { CategoryId = category.Id },
            autoSave: true);

        // A topic on every question, so the section breakdown is proved to sit
        // beside the competency one rather than to have replaced it.
        _topicId = (await topics.InsertAsync(
            new Topic(Guid.NewGuid(), Tenant, code + "-t", code), autoSave: true)).Id;

        return await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
            LevelId = level.Id,
        });
    }

    private Guid _topicId;

    private async Task BankAsync(Guid examId, string code, Guid? sectionId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = examId,
                ExamSectionId = sectionId,
                TopicId = _topicId,
                Type = QuestionTypes.SingleChoice,
                Text = code + " question " + (i + 1),
                Score = 1m,
                DisplayOrder = i,
                Payload = PayloadJson.Write(new ChoicePayload
                {
                    Options =
                    [
                        new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                        new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
                    ],
                }),
            });
        }
    }

    private async Task SetDrawAsync(Guid examId, string name, int questionsPerForm)
    {
        var section = (await _structure.GetSectionsAsync(examId)).Single(s => s.Name == name);

        await _structure.UpdateSectionAsync(section.Id, new CreateUpdateExamSectionDto
        {
            ExamId = examId,
            Name = section.Name,
            Instructions = section.Instructions,
            DisplayOrder = section.DisplayOrder,
            QuestionsPerForm = questionsPerForm,
        });
    }

    private async Task<string> SendAsync(Guid examId, string email)
    {
        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = email.Split('@')[0],
            Email = email,
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = examId,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        return result.Recipients.Single().Url.Split('/').Last();
    }

    /// <summary>The paper as the candidate sees it, in the order they see it.</summary>
    private async Task<List<TakerQuestionDto>> PaperAsync(string linkToken)
    {
        var preview = await _taking.OpenLinkAsync(linkToken);

        preview.IsAccessible.ShouldBeTrue(preview.BlockReason);

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
}
