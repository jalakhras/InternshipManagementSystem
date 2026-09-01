using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.Media;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// A candidate storing their own answer file.
/// <para>
/// Uploading was staff-only, so the two question types whose whole answer <i>is</i>
/// a file — an uploaded document and a recorded spoken answer — could not be
/// answered at all. A speaking test with no way to record is not a speaking test.
/// </para>
/// <para>
/// This is also the only door in the product an unauthenticated stranger can
/// push bytes through, so most of what is asserted here is what it refuses.
/// </para>
/// </summary>
public class AnswerUploadTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly IAssessmentMediaAppService _media;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000101");

    public AnswerUploadTests()
    {
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _media = GetRequiredService<IAssessmentMediaAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task A_candidate_can_store_their_own_answer_file()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("upload-a");

            var stored = await _media.UploadAnswerAsync(File("essay.pdf"), sitting.SessionToken);

            stored.BlobName.ShouldNotBeNullOrWhiteSpace();
            stored.OriginalFileName.ShouldBe("essay.pdf");

            // Filed under the attempt that produced it. That is what makes an
            // uploaded answer traceable to a sitting when somebody disputes a
            // mark — and nothing in the path came from the caller: the tenant and
            // the attempt are read off the signed token.
            stored.BlobName.ShouldContain(sitting.AttemptId.ToString("N"));
        });
    }

    [Fact]
    public async Task The_stored_file_is_attached_to_the_answer()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("upload-b");
            var question = await _taking.GetQuestionAsync(sitting.SessionToken, 0);

            var stored = await _media.UploadAnswerAsync(File("report.pdf"), sitting.SessionToken);

            await _taking.SaveAnswerAsync(sitting.SessionToken, new SaveAnswerDto
            {
                QuestionId = question.Id,
                AnswerBlobName = stored.BlobName,
                AnswerFileName = stored.OriginalFileName,
                TimeSpentSeconds = 60,
            });

            var answers = GetRequiredService<IRepository<Answer, Guid>>();
            var saved = (await answers.GetListAsync(a => a.AttemptId == sitting.AttemptId)).Single();

            // Storing the bytes is half of it. An upload the marker never sees is
            // the same as no answer.
            saved.AnswerBlobName.ShouldBe(stored.BlobName);
            saved.AnswerFileName.ShouldBe("report.pdf");
        });
    }

    [Fact]
    public async Task A_stranger_with_no_session_stores_nothing()
    {
        await AsTenantAsync(async () =>
        {
            // A business refusal rather than an authorization one, and the type is
            // not a detail: an authorization failure on an unauthenticated request
            // makes ASP.NET Core challenge the default scheme — a cookie here — so
            // the answer to a candidate's expired session was 302 to a staff
            // sign-in page. The exception type decided that, not any line of code.
            var refusal = await Should.ThrowAsync<BusinessException>(async () =>
                await _media.UploadAnswerAsync(File("anything.pdf"), "not-a-token"));

            refusal.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamSessionExpired);
        });
    }

    [Fact]
    public async Task A_session_from_the_entry_screen_stores_nothing()
    {
        await AsTenantAsync(async () =>
        {
            var token = await SendAsync("upload-c");
            var preview = await _taking.OpenLinkAsync(token);

            // The entry screen's session names no attempt — the exam has not been
            // started. There is nothing to attach a file to, and accepting one
            // would let anybody holding a link write to disk without ever sitting.
            var refusal = await Should.ThrowAsync<BusinessException>(async () =>
                await _media.UploadAnswerAsync(File("early.pdf"), preview.SessionToken!));

            refusal.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamNotStarted);
        });
    }

    [Fact]
    public async Task A_kind_of_file_an_answer_is_never_made_of_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("upload-d");

            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _media.UploadAnswerAsync(File("payload.exe"), sitting.SessionToken));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.FileTypeNotAllowed);
        });
    }

    [Fact]
    public async Task A_file_too_large_to_be_an_answer_is_refused()
    {
        await AsTenantAsync(async () =>
        {
            var sitting = await SitAsync("upload-e");

            var thrown = await Should.ThrowAsync<BusinessException>(async () =>
                await _media.UploadAnswerAsync(
                    File("huge.pdf", bytes: 11 * 1024 * 1024), sitting.SessionToken));

            thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.FileTooLarge);
        });
    }

    // ------------------------------------------------------------------ helpers

    private static IFormFile File(string name, int bytes = 64)
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes(new string('a', bytes)));

        // Headers are set even though nothing here reads them: FormFile computes
        // ContentDisposition from them on demand and throws a NullReference
        // without, which surfaces as a TargetInvocationException from the
        // interceptor and looks like a fault in the service under test.
        return new FormFile(content, 0, content.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };
    }

    private sealed record Sitting(Guid AttemptId, string SessionToken);

    private async Task<Sitting> SitAsync(string code)
    {
        var token = await SendAsync(code);
        var preview = await _taking.OpenLinkAsync(token);
        var state = await _taking.StartAsync(preview.SessionToken!);

        return new Sitting(state.AttemptId, state.SessionToken!);
    }

    private async Task<string> SendAsync(string code)
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
            Type = QuestionTypes.FileUpload,
            Text = code + " — upload your work",
            Score = 5m,
            Payload = PayloadJson.Write(new RubricPayload()),
        });

        await _exams.PublishAsync(exam.Id);

        var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
        {
            FullName = "Uploads their work",
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
