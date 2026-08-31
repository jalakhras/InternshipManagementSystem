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
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// Fixtures for permission tests: an exam that exists, and a sitting of it.
/// <para>
/// Every fixture here is built under <c>GrantEverything()</c>. Creating an exam,
/// publishing it, adding a candidate and sending a link needs eight permissions
/// that are never the subject of the test, and building the fixture with a narrow
/// grant would mean a refusal could come from the setup rather than from the call
/// being examined.
/// </para>
/// </summary>
public abstract class AssessmentPermissionTestBase : PermissionEnforcedTestBase
{
    protected readonly IExamAppService Exams;
    protected readonly IQuestionAppService Questions;
    protected readonly ICandidateAppService Candidates;
    protected readonly IAssignmentAppService Assignments;
    protected readonly IExamTakingAppService Taking;
    protected readonly ICurrentTenant CurrentTenant;

    protected static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000a1");

    protected AssessmentPermissionTestBase()
    {
        Exams = GetRequiredService<IExamAppService>();
        Questions = GetRequiredService<IQuestionAppService>();
        Candidates = GetRequiredService<ICandidateAppService>();
        Assignments = GetRequiredService<IAssignmentAppService>();
        Taking = GetRequiredService<IExamTakingAppService>();
        CurrentTenant = GetRequiredService<ICurrentTenant>();
    }

    protected async Task AsTenantAsync(Func<Task> action)
    {
        using (CurrentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }

    /// <summary>A published exam with one free-text question, so a mark needs a person.</summary>
    protected async Task<Guid> PublishedExamAsync(string code)
    {
        var categories = GetRequiredService<IRepository<Category, Guid>>();

        var category = await categories.InsertAsync(
            new Category(Guid.NewGuid(), Tenant, code, code), autoSave: true);

        var exam = await Exams.CreateAsync(new CreateUpdateExamDto
        {
            Title = code,
            TimeLimitInMinutes = 30,
            PassingPercentage = 50m,
            CategoryId = category.Id,
        });

        await Questions.CreateAsync(new CreateUpdateQuestionDto
        {
            ExamId = exam.Id,
            Type = QuestionTypes.Text,
            Text = code + " — explain your reasoning",
            Score = 20m,
            Payload = PayloadJson.Write(new RubricPayload()),
        });

        await Exams.PublishAsync(exam.Id);

        return exam.Id;
    }

    /// <summary>A candidate, a link and a started sitting. Returns the session token.</summary>
    protected async Task<StartedSitting> StartedSittingAsync(Guid examId, string code)
    {
        var candidate = await Candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Sat " + code,
            Email = code + "@example.test",
        });

        var sent = await Assignments.CreateAsync(new CreateAssignmentDto
        {
            ExamId = examId,
            CandidateId = candidate.Id,
            ExpiresAt = DateTime.Now.AddDays(7),
            MaxAttempts = 1,
            SendEmail = false,
        });

        var token = sent.Recipients.Single().Url.Split('/').Last();
        var preview = await Taking.OpenLinkAsync(token);
        var started = await Taking.StartAsync(preview.SessionToken!);

        return new StartedSitting(started.AttemptId, started.SessionToken!, candidate.Id, code + "@example.test");
    }

    /// <summary>Sits the exam and submits it, leaving one answer waiting on a marker.</summary>
    protected async Task<StartedSitting> SatAndSubmittedAsync(Guid examId, string code)
    {
        var sitting = await StartedSittingAsync(examId, code);

        var question = await Taking.GetQuestionAsync(sitting.SessionToken, 0);

        await Taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
        {
            QuestionId = question.Id,
            Response = "Because the volume dried up at the high.",
            TimeSpentSeconds = 200,
            KeystrokeCount = 60,
            BackspaceCount = 5,
        });

        await Taking.SubmitAsync(sitting.SessionToken);

        return sitting;
    }

    protected sealed record StartedSitting(Guid AttemptId, string SessionToken, Guid CandidateId, string Email);
}
