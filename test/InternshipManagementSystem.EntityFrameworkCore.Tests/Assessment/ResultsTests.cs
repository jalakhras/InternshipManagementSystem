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
using InternshipManagementSystem.Assessment.Results.Dtos;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Whether the person who paid for the exam can see what it produced.
/// <para>
/// Until this was written they could not. Every permission existed —
/// <c>Results.View</c>, <c>.Export</c>, <c>.ViewItemAnalysis</c> — and behind
/// them was nothing at all. The only results screen was the manual-marking queue,
/// which filters to sittings a person still has to mark, so an all-multiple-
/// choice paper was graded in milliseconds and then appeared nowhere.
/// </para>
/// <para>
/// So these tests run a real sitting end to end and then ask the question a
/// coordinator asks: who passed.
/// </para>
/// </summary>
public class ResultsTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IResultAppService _results;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000031");

    public ResultsTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _results = GetRequiredService<IResultAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task An_auto_marked_sitting_shows_up_in_the_results()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-a");

            await SitAsync(exam, "passer@example.test", answerCorrectly: true);

            var rows = await _results.GetListAsync(new ResultListRequestDto { ExamId = exam.ExamId });

            // The case the product could not show at all: nothing on this paper
            // needs a human, so it never reached the review queue and there was no
            // other screen.
            rows.TotalCount.ShouldBe(1);
            rows.Items.Single().CandidateEmail.ShouldBe("passer@example.test");
            rows.Items.Single().IsGraded.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task A_correct_paper_passes_and_an_empty_one_does_not()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-b");

            await SitAsync(exam, "right@example.test", answerCorrectly: true);
            await SitAsync(exam, "blank@example.test", answerCorrectly: false);

            var rows = await _results.GetListAsync(new ResultListRequestDto { ExamId = exam.ExamId });

            var passed = rows.Items.Single(r => r.CandidateEmail == "right@example.test");
            var failed = rows.Items.Single(r => r.CandidateEmail == "blank@example.test");

            passed.IsPassed.ShouldBeTrue();
            passed.ScorePercentage.ShouldBe(100m);

            failed.IsPassed.ShouldBeFalse();
            failed.Score.ShouldBe(0m);
        });
    }

    [Fact]
    public async Task The_summary_counts_the_cohort_rather_than_the_page()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-c");

            await SitAsync(exam, "one@example.test", answerCorrectly: true);
            await SitAsync(exam, "two@example.test", answerCorrectly: true);
            await SitAsync(exam, "three@example.test", answerCorrectly: false);

            var summary = await _results.GetSummaryAsync(new ResultListRequestDto
            {
                ExamId = exam.ExamId,

                // Deliberately a page smaller than the cohort. "Two thirds passed"
                // is a statement about the group, and a page-sized version of it
                // would change every time somebody turned a page.
                MaxResultCount = 1,
            });

            summary.Sat.ShouldBe(3);
            summary.Passed.ShouldBe(2);
            summary.Failed.ShouldBe(1);
        });
    }

    [Fact]
    public async Task Somebody_who_never_opened_their_link_is_counted_separately()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-d");

            await SitAsync(exam, "came@example.test", answerCorrectly: true);
            await SendAsync(exam.ExamId, "never@example.test");

            var summary = await _results.GetSummaryAsync(new ResultListRequestDto { ExamId = exam.ExamId });

            // The number a coordinator chases, and one no attempt row can carry
            // because the whole point is that there is no attempt.
            summary.Sat.ShouldBe(1);
            summary.NotStarted.ShouldBe(1);
        });
    }

    [Fact]
    public async Task A_result_breaks_the_score_down_by_topic()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-e", withTopics: true);

            var attemptId = await SitAsync(exam, "detail@example.test", answerCorrectly: true);

            var detail = await _results.GetAsync(attemptId);

            // The reason a result is worth more than a number. "Strong on grammar,
            // weak on listening" is something a training centre can act on.
            detail.ByTopic.ShouldNotBeEmpty();
            detail.ByTopic.ShouldContain(t => t.TopicName == "Grammar");
            detail.Answers.Count.ShouldBe(exam.QuestionIds.Count);
        });
    }

    [Fact]
    public async Task The_export_quotes_a_name_that_contains_a_comma()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-f");

            await SitAsync(exam, "comma@example.test", answerCorrectly: true, fullName: "Smith, John");

            var csv = await _results.ExportCsvAsync(new ResultListRequestDto { ExamId = exam.ExamId });

            // Unquoted, this name shifts every column after it by one and the marks
            // end up against the wrong people — silently, in a spreadsheet nobody
            // re-checks.
            csv.ShouldContain("\"Smith, John\"");
        });
    }

    [Fact]
    public async Task Item_analysis_flags_a_question_everybody_gets_right()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("results-g");

            // Twenty is the floor the flagging uses. Below it the numbers are noise,
            // and flagging noise teaches an author to ignore the flags.
            for (var i = 0; i < 20; i++)
            {
                await SitAsync(exam, $"cohort{i}@example.test", answerCorrectly: true);
            }

            var analysis = await _results.GetItemAnalysisAsync(exam.ExamId);

            analysis.ShouldNotBeEmpty();
            analysis.ShouldAllBe(row => row.Facility == 1m);
            analysis.ShouldContain(row => row.FlagKey == "IMS:ItemAnalysis:TooEasy");
        });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record ExamFixture(Guid ExamId, System.Collections.Generic.List<Guid> QuestionIds);

    /// <summary>A published two-question exam, optionally with its questions filed under topics.</summary>
    private async Task<ExamFixture> ExamAsync(string code, bool withTopics = false)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();
        var topics = GetRequiredService<IRepository<Topic, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        Guid? grammar = null;

        if (withTopics)
        {
            var topic = await topics.InsertAsync(
                new Topic(Guid.NewGuid(), Tenant, code + "-grammar", "Grammar") { CategoryId = category.Id },
                autoSave: true);

            grammar = topic.Id;
        }

        var exam = await _exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
        });

        var questionIds = new System.Collections.Generic.List<Guid>();

        for (var i = 0; i < 2; i++)
        {
            var question = await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
                TopicId = grammar,
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
            });

            questionIds.Add(question.Id);
        }

        await _exams.PublishAsync(exam.Id);

        return new ExamFixture(exam.Id, questionIds);
    }

    /// <summary>Sends a link, sits the exam, and submits. Returns the attempt id.</summary>
    private async Task<Guid> SitAsync(
        ExamFixture exam,
        string email,
        bool answerCorrectly,
        string? fullName = null)
    {
        var token = await SendAsync(exam.ExamId, email, fullName);

        var preview = await _taking.OpenLinkAsync(token);
        var state = await _taking.StartAsync(preview.SessionToken!);
        var session = state.SessionToken!;

        if (answerCorrectly)
        {
            for (var position = 0; position < state.TotalQuestions; position++)
            {
                var question = await _taking.GetQuestionAsync(session, position);

                // Through the taker's own view of the paper, so the option order
                // this candidate was shown is the one being answered against.
                var correct = question.Options.Single(o => o.Text == "Right").Id;

                await _taking.SaveAnswerAsync(session, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = PayloadJson.Write(new[] { correct }),
                });
            }
        }

        await _taking.SubmitAsync(session);

        return state.AttemptId;
    }

    private async Task<string> SendAsync(Guid examId, string email, string? fullName = null)
    {
        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = fullName ?? email.Split('@')[0],
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

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
