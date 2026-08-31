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
using System.Text;
using Volo.Abp.BlobStoring;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// A coordinator standing next to somebody whose browser has frozen.
/// <para>
/// <c>Attempts.View</c>, <c>.ForceSubmit</c> and <c>.Delete</c> were three
/// grantable permissions that enforced nothing, because nothing implemented
/// them. The moment they describe is ordinary and the product had no answer to
/// it: the coordinator could see neither that the sitting was live nor any way
/// to end it.
/// </para>
/// </summary>
public class AttemptAdminTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IAttemptAdminAppService _admin;
    private readonly IResultAppService _results;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000041");

    public AttemptAdminTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _admin = GetRequiredService<IAttemptAdminAppService>();
        _results = GetRequiredService<IResultAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_sitting_in_progress_is_visible_while_it_is_happening()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-a");
            await StartAsync(exam, "live@example.test");

            var running = await _admin.GetRunningAsync(new RunningAttemptRequestDto());

            // The whole point: somebody is in the room right now, and until this
            // existed no screen in the product could say so.
            running.Items.ShouldContain(row => row.CandidateEmail == "live@example.test");
        });
    }

    [Fact]
    public async Task A_submitted_sitting_is_not_listed_as_running()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-b");
            var session = await StartAsync(exam, "done@example.test");

            await _taking.SubmitAsync(session);

            var running = await _admin.GetRunningAsync(new RunningAttemptRequestDto());

            running.Items.ShouldNotContain(row => row.CandidateEmail == "done@example.test");
        });
    }

    [Fact]
    public async Task Ending_a_sitting_marks_what_was_answered()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-c");
            var session = await StartAsync(exam, "frozen@example.test");

            // One of two questions answered, correctly, before the browser froze.
            var first = await _taking.GetQuestionAsync(session, 0);

            await _taking.SaveAnswerAsync(session, new SaveAnswerDto
            {
                QuestionId = first.Id,
                Response = PayloadJson.Write(new[] { first.Options.Single(o => o.Text == "Right").Id }),
            });

            var row = await _admin.ForceSubmitAsync(
                AttemptIdFor("frozen@example.test"),
                new ForceSubmitDto { Reason = "Browser froze." });

            // Half the paper answered, so half the marks. What the candidate did
            // before they were stopped counts in full — the reason they stopped is
            // not their score's problem.
            row.IsGraded.ShouldBeTrue();
            row.Score.ShouldBe(1m);
            row.MaxScore.ShouldBe(2m);
            row.EndReason.ShouldBe(nameof(AttemptEndReason.EndedByAdministrator));
        });
    }

    [Fact]
    public async Task Ending_a_sitting_records_why()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-d");
            await StartAsync(exam, "why@example.test");

            var attemptId = AttemptIdFor("why@example.test");

            await _admin.ForceSubmitAsync(attemptId, new ForceSubmitDto
            {
                Reason = "Fire alarm; the room was cleared.",
            });

            var attempts = GetRequiredService<IRepository<Attempt, Guid>>();

            // Ending somebody's exam early gets questioned weeks later, and "the
            // system did it" is not an answer anybody can defend.
            (await attempts.GetAsync(attemptId)).EndedByReason
                .ShouldBe("Fire alarm; the room was cleared.");
        });
    }

    [Fact]
    public async Task A_sitting_that_has_already_ended_cannot_be_ended_again()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-e");
            var session = await StartAsync(exam, "twice@example.test");

            await _taking.SubmitAsync(session);

            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _admin.ForceSubmitAsync(
                    AttemptIdFor("twice@example.test"), new ForceSubmitDto()));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.AttemptAlreadySubmitted);
        });
    }

    [Fact]
    public async Task A_marked_attempt_cannot_be_discarded()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-f");
            var session = await StartAsync(exam, "marked@example.test");

            await _taking.SubmitAsync(session);

            // A graded attempt is somebody's result. Removing one is not a
            // correction, it is a disappearance — and the person who sat it has no
            // way to know it happened.
            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _admin.DeleteAsync(AttemptIdFor("marked@example.test")));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.AttemptGradedCannotDelete);
        });
    }

    [Fact]
    public async Task A_running_attempt_can_be_discarded_with_everything_it_recorded()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-g");
            await StartAsync(exam, "testrun@example.test");

            var attemptId = AttemptIdFor("testrun@example.test");

            await _admin.DeleteAsync(attemptId);

            var slots = GetRequiredService<IRepository<AttemptQuestion, Guid>>();

            // The paper goes with it. Rows pointing at an attempt that no longer
            // exists are the kind of debris that makes a later count wrong.
            (await slots.GetQueryableAsync()).Any(q => q.AttemptId == attemptId).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Discarding_an_attempt_takes_what_was_observed_about_the_candidate()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-h");
            var session = await StartAsync(exam, "watched@example.test");

            await _taking.ReportSignalAsync(session, new ReportIntegritySignalDto
            {
                Type = IntegritySignalType.WindowBlur,
                Magnitude = 12,
            });

            var attemptId = AttemptIdFor("watched@example.test");

            await _admin.DeleteAsync(attemptId);

            var signals = GetRequiredService<IRepository<IntegritySignal, Guid>>();

            // The dialog says everything it recorded is removed, and these are
            // literally the recordings: what the candidate pasted, when they left
            // the window, how long they took. They are observations about a person
            // made while nobody was looking, and they outlived the sitting they
            // describe — pointing at an attempt that no longer exists, so nothing
            // could ever explain them again.
            (await signals.GetQueryableAsync()).Any(x => x.AttemptId == attemptId).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Discarding_an_attempt_takes_the_answers_that_were_written()
    {
        await AsTenantAsync(async () =>
        {
            var exam = await ExamAsync("monitor-i");
            var session = await StartAsync(exam, "wrote@example.test");

            var question = await _taking.GetQuestionAsync(session, 0);

            await _taking.SaveAnswerAsync(session, new SaveAnswerDto
            {
                QuestionId = question.Id,
                Response = "something written before it was discarded",
            });

            var attemptId = AttemptIdFor("wrote@example.test");

            await _admin.DeleteAsync(attemptId);

            var answers = GetRequiredService<IRepository<Answer, Guid>>();

            (await answers.GetQueryableAsync()).Any(a => a.AttemptId == attemptId).ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Discarding_an_attempt_takes_the_file_the_candidate_uploaded()
    {
        await AsTenantAsync(async () =>
        {
            var blobs = GetRequiredService<IBlobContainer<AssessmentBlobContainer>>();

            var exam = await ExamAsync("monitor-j");
            var session = await StartAsync(exam, "recorded@example.test");

            var question = await _taking.GetQuestionAsync(session, 0);
            var attemptId = AttemptIdFor("recorded@example.test");

            var name = Tenant + "/answers/" + attemptId + "/spoken.webm";

            await blobs.SaveAsync(name, Encoding.UTF8.GetBytes("a minute of somebody speaking"));

            await _taking.SaveAnswerAsync(session, new SaveAnswerDto
            {
                QuestionId = question.Id,
                AnswerBlobName = name,
                AnswerFileName = "spoken.webm",
            });

            await _admin.DeleteAsync(attemptId);

            // The row is not the recording. Deleting the row that names a file and
            // leaving the file is worse than not deleting at all: the recording of
            // somebody's voice stays on disk, and nothing is left that could find
            // it again to finish the job.
            (await blobs.ExistsAsync(name)).ShouldBeFalse();
        });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record ExamFixture(Guid Id);

    private async Task<ExamFixture> ExamAsync(string code)
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

        for (var i = 0; i < 2; i++)
        {
            await _questions.CreateAsync(new CreateUpdateQuestionDto
            {
                ExamId = exam.Id,
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

        await _exams.PublishAsync(exam.Id);

        return new ExamFixture(exam.Id);
    }

    /// <summary>Sends a link and starts the sitting. Returns the session token.</summary>
    private async Task<string> StartAsync(ExamFixture exam, string email)
    {
        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = email.Split('@')[0],
            Email = email,
        });

        var result = await _assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = exam.Id,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        var token = result.Recipients.Single().Url.Split('/').Last();
        var preview = await _taking.OpenLinkAsync(token);

        return (await _taking.StartAsync(preview.SessionToken!)).SessionToken!;
    }

    /// <summary>The attempt this candidate is sitting, found the way a coordinator finds it.</summary>
    private Guid AttemptIdFor(string email)
    {
        var running = _admin.GetRunningAsync(new RunningAttemptRequestDto { Filter = email })
            .GetAwaiter().GetResult();

        if (running.Items.Count > 0)
        {
            return running.Items[0].AttemptId;
        }

        // Already finished, so it is no longer running and has to be found among
        // the results instead.
        return _results.GetListAsync(new ResultListRequestDto { Filter = email })
            .GetAwaiter().GetResult().Items[0].AttemptId;
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
