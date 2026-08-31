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
using Shouldly;
using System.Text;
using Volo.Abp.BlobStoring;
using Volo.Abp.Identity;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// What actually goes when an organisation is deleted.
/// <para>
/// The dialog says it in both languages, in the sentence somebody reads while
/// they are steadying themselves to type a name back: "Everything this
/// organisation owns will go — its exams, its questions, its people, and every
/// result they ever sat. This cannot be undone."
/// </para>
/// <para>
/// Nothing carried it out. Deleting a tenant removed the tenant row and left
/// nineteen tables holding that tenant's id — candidates by name and address,
/// their attempts, their answers, the integrity observations recorded while
/// they sat. None of it is soft-deleted, so there was not even a row marked as
/// gone; the rows simply stayed, pointing at an organisation that no longer
/// existed.
/// </para>
/// <para>
/// This is the recurring defect of this product in its worst form, because the
/// promise is about somebody else's personal data. A centre leaves and asks for
/// its people's records to be erased; an administrator types the name, reads
/// "cannot be undone", and the names, the email addresses and the answers are
/// all still there. Re-creating an organisation with the same name gets a new
/// id, so the old rows never resurface and nobody ever finds out.
/// </para>
/// </summary>
public class TenantDeletionTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ITenantAppService _tenants;
    private readonly IExamAppService _exams;
    private readonly IQuestionAppService _questions;
    private readonly ICandidateAppService _candidates;
    private readonly IAssignmentAppService _assignments;
    private readonly IExamTakingAppService _taking;
    private readonly ICurrentTenant _currentTenant;

    public TenantDeletionTests()
    {
        _tenants = GetRequiredService<ITenantAppService>();
        _exams = GetRequiredService<IExamAppService>();
        _questions = GetRequiredService<IQuestionAppService>();
        _candidates = GetRequiredService<ICandidateAppService>();
        _assignments = GetRequiredService<IAssignmentAppService>();
        _taking = GetRequiredService<IExamTakingAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Deleting_an_organisation_takes_the_people_it_held()
    {
        var tenantId = await LivedInAsync("leaving-a");

        await WithUnitOfWorkAsync(async () => await _tenants.DeleteAsync(tenantId));

        // Names and email addresses of real people, which is what the dialog
        // promises to erase and what a data-protection request is about.
        (await CountAsync<Candidate>(tenantId)).ShouldBe(0);
        (await CountAsync<CandidateGroup>(tenantId)).ShouldBe(0);
        (await CountAsync<CandidateGroupMember>(tenantId)).ShouldBe(0);
    }

    [Fact]
    public async Task Deleting_an_organisation_takes_every_result_they_ever_sat()
    {
        var tenantId = await LivedInAsync("leaving-b");

        await WithUnitOfWorkAsync(async () => await _tenants.DeleteAsync(tenantId));

        // The sentence names these one by one, and an answer is the most
        // personal row in the product: what somebody wrote, under a clock, to be
        // judged on.
        (await CountAsync<Attempt>(tenantId)).ShouldBe(0);
        (await CountAsync<AttemptQuestion>(tenantId)).ShouldBe(0);
        (await CountAsync<Answer>(tenantId)).ShouldBe(0);
        (await CountAsync<IntegritySignal>(tenantId)).ShouldBe(0);
        (await CountAsync<ExamLink>(tenantId)).ShouldBe(0);
        (await CountAsync<Assignment>(tenantId)).ShouldBe(0);
    }

    [Fact]
    public async Task Deleting_an_organisation_takes_its_exams_and_its_questions()
    {
        var tenantId = await LivedInAsync("leaving-c");

        await WithUnitOfWorkAsync(async () => await _tenants.DeleteAsync(tenantId));

        (await CountAsync<Exam>(tenantId)).ShouldBe(0);
        (await CountAsync<Question>(tenantId)).ShouldBe(0);
        (await CountAsync<ExamSection>(tenantId)).ShouldBe(0);
        (await CountAsync<ExamBlueprintRule>(tenantId)).ShouldBe(0);

        // The catalogue too. It is the organisation's own vocabulary — its
        // languages, its levels, its competencies — and it means nothing without
        // the organisation.
        (await CountAsync<Category>(tenantId)).ShouldBe(0);
        (await CountAsync<Level>(tenantId)).ShouldBe(0);
        (await CountAsync<Topic>(tenantId)).ShouldBe(0);
    }

    [Fact]
    public async Task Deleting_one_organisation_leaves_the_others_untouched()
    {
        var leaving = await LivedInAsync("leaving-d");
        var staying = await LivedInAsync("staying-d");

        await WithUnitOfWorkAsync(async () => await _tenants.DeleteAsync(leaving));

        // The half that makes the deletion safe to have at all. A cascade that
        // reaches past its own tenant is worse than no cascade: it takes a
        // paying customer's exams with a departing one's.
        (await CountAsync<Candidate>(staying)).ShouldBeGreaterThan(0);
        (await CountAsync<Exam>(staying)).ShouldBeGreaterThan(0);
        (await CountAsync<Answer>(staying)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Deleting_an_organisation_takes_the_files_its_people_uploaded()
    {
        var blobs = GetRequiredService<IBlobContainer<AssessmentBlobContainer>>();

        var tenantId = await LivedInAsync("leaving-e");

        var name = tenantId + "/answers/spoken.webm";

        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantId))
            {
                await blobs.SaveAsync(name, Encoding.UTF8.GetBytes("a minute of somebody speaking"));

                var answers = GetRequiredService<IRepository<Answer, Guid>>();
                var answer = (await answers.GetListAsync()).First();

                answer.AnswerBlobName = name;

                await answers.UpdateAsync(answer, autoSave: true);
            }
        });

        await WithUnitOfWorkAsync(async () => await _tenants.DeleteAsync(tenantId));

        // The row is not the data. A candidate's recorded answer is the most
        // personal thing this product holds, and deleting the row that names it
        // leaves the recording on disk with nothing left pointing at it — which
        // is worse than not deleting at all, because now nobody can find it to
        // finish the job.
        await WithUnitOfWorkAsync(async () =>
        {
            using (_currentTenant.Change(tenantId))
            {
                (await blobs.ExistsAsync(name)).ShouldBeFalse();
            }
        });
    }

    [Fact]
    public async Task Deleting_an_organisation_takes_the_accounts_its_staff_signed_in_with()
    {
        var tenantId = await LivedInAsync("leaving-f");

        await WithUnitOfWorkAsync(async () => await _tenants.DeleteAsync(tenantId));

        // Staff accounts are personal data too: a name, an address, and a hash of
        // a password people reuse. The dialog says everything the organisation
        // owns goes, and the administrator created with it is the first thing it
        // was given.
        var left = await WithUnitOfWorkAsync(async () =>
        {
            using (GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
            {
                var users = GetRequiredService<IRepository<IdentityUser, Guid>>();

                return (await users.GetListAsync()).Count(u => u.TenantId == tenantId);
            }
        });

        left.ShouldBe(0);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Counted with the tenant filter off, because the point of the assertion is
    /// that the rows are gone rather than merely out of view. A count taken
    /// inside the filter would pass on a tenant that no longer exists whether or
    /// not anything was deleted, which is the shape of a test that cannot fail.
    /// </summary>
    private async Task<int> CountAsync<TEntity>(Guid tenantId) where TEntity : class, IEntity, IMultiTenant
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            using (GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
            {
                var repository = GetRequiredService<IRepository<TEntity>>();
                var queryable = await repository.GetQueryableAsync();

                return queryable.Count(e => e.TenantId == tenantId);
            }
        });
    }

    /// <summary>An organisation somebody actually used: a paper written, sent, sat and answered.</summary>
    private async Task<Guid> LivedInAsync(string code)
    {
        var tenant = await WithUnitOfWorkAsync(async () =>
            await _tenants.CreateAsync(new TenantCreateDto
            {
                Name = code,
                AdminEmailAddress = code + "@example.test",
                AdminPassword = "1q2w3E*",
            }));

        using (_currentTenant.Change(tenant.Id))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var categories = GetRequiredService<IRepository<Category, Guid>>();
                var levels = GetRequiredService<IRepository<Level, Guid>>();
                var topics = GetRequiredService<IRepository<Topic, Guid>>();

                var category = await categories.InsertAsync(
                    new Category(Guid.NewGuid(), tenant.Id, code, code), autoSave: true);

                await levels.InsertAsync(
                    new Level(Guid.NewGuid(), tenant.Id, code + "-1", code) { CategoryId = category.Id },
                    autoSave: true);

                await topics.InsertAsync(
                    new Topic(Guid.NewGuid(), tenant.Id, code + "-t", code) { CategoryId = category.Id },
                    autoSave: true);

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
                    Type = QuestionTypes.Text,
                    Text = code + " — write something",
                    Score = 10m,
                    Payload = PayloadJson.Write(new RubricPayload()),
                });

                await _exams.PublishAsync(exam.Id);

                var candidate = await _candidates.CreateAsync(new CreateUpdateCandidateDto
                {
                    FullName = "سارة العتيبي",
                    Email = code + "-sat@example.test",
                });

                var group = await _candidates.CreateGroupAsync(new CreateUpdateCandidateGroupDto { Name = code + " class" });

                await _candidates.ChangeGroupMembersAsync(group.Id, new ChangeGroupMembersDto
                {
                    Add = [candidate.Id],
                });

                var sent = await _assignments.CreateAsync(new CreateAssignmentDto
                {
                    ExamId = exam.Id,
                    CandidateId = candidate.Id,
                    ExpiresAt = DateTime.Now.AddDays(7),
                    MaxAttempts = 1,
                    SendEmail = false,
                });

                var token = sent.Recipients.Single().Url.Split('/').Last();
                var preview = await _taking.OpenLinkAsync(token);
                var state = await _taking.StartAsync(preview.SessionToken!);
                var question = await _taking.GetQuestionAsync(state.SessionToken!, 0);

                await _taking.SaveAnswerAsync(state.SessionToken!, new SaveAnswerDto
                {
                    QuestionId = question.Id,
                    Response = "ما كتبته تحت ساعةٍ تعمل.",
                    TimeSpentSeconds = 120,
                    KeystrokeCount = 40,
                });

                await _taking.SubmitAsync(state.SessionToken!);
            });
        }

        return tenant.Id;
    }
}
