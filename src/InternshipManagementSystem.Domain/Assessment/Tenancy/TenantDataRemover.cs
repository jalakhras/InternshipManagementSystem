using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace InternshipManagementSystem.Assessment.Tenancy;

/// <summary>
/// Carrying out what the delete dialog promises.
/// <para>
/// The dialog says it in both languages, in the sentence somebody reads while
/// steadying themselves to type a name back: everything this organisation owns
/// will go — its exams, its questions, its people, and every result they ever
/// sat. Nothing carried it out. Deleting an organisation removed one row from
/// one table and left nineteen others holding its id.
/// </para>
/// <para>
/// That is the worst instance of this product's recurring defect, because the
/// promise is about other people's personal data. A centre leaves and asks for
/// its candidates' records to be erased; an administrator types the name, reads
/// "this cannot be undone", and the names, the addresses and the answers are
/// all still in the database. Nobody ever finds out: an organisation recreated
/// under the same name gets a new id, so the old rows never surface again.
/// </para>
/// <para>
/// Order matters. Children before parents, so a row is never left pointing at
/// something that has gone — and if the deletion fails half way the rest is
/// still consistent enough to try again.
/// </para>
/// </summary>
public class TenantDataRemover
    : ILocalEventHandler<EntityDeletedEventData<Tenant>>, ITransientDependency
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWork;
    private readonly IBlobContainer<AssessmentBlobContainer> _blobs;
    private readonly IServiceProvider _services;
    private readonly ILogger<TenantDataRemover> _logger;

    public TenantDataRemover(
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWork,
        IBlobContainer<AssessmentBlobContainer> blobs,
        IServiceProvider services,
        ILogger<TenantDataRemover> logger)
    {
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _blobs = blobs;
        _services = services;
        _logger = logger;
    }

    public async Task HandleEventAsync(EntityDeletedEventData<Tenant> eventData)
    {
        var tenantId = eventData.Entity.Id;

        using (_currentTenant.Change(tenantId))
        {
            // Read before anything is deleted, because a blob's address is stored
            // on the row that refers to it and there is no way to list a
            // container by prefix. Delete the rows first and the files stay on
            // disk for ever with nothing left pointing at them — and a
            // candidate's recorded answer is the most personal thing this product
            // holds.
            var files = await FilesOfAsync(tenantId);

            // Deepest first. An answer belongs to an attempt, an attempt to a
            // link, a link to an assignment; deleting from the top would leave
            // rows pointing at nothing for as long as the deletion takes, and
            // for good if it fails part way.
            await RemoveAsync<Answer>(tenantId);
            await RemoveAsync<IntegritySignal>(tenantId);
            await RemoveAsync<AttemptQuestion>(tenantId);
            await RemoveAsync<Attempt>(tenantId);
            await RemoveAsync<ExamLink>(tenantId);
            await RemoveAsync<Assignment>(tenantId);

            await RemoveAsync<CandidateGroupMember>(tenantId);
            await RemoveAsync<CandidateGroup>(tenantId);
            await RemoveAsync<Candidate>(tenantId);

            await RemoveAsync<ExamFormQuestion>(tenantId);
            await RemoveAsync<ExamForm>(tenantId);
            await RemoveAsync<ExamBlueprintRule>(tenantId);
            await RemoveAsync<Question>(tenantId);
            await RemoveAsync<QuestionGroup>(tenantId);
            await RemoveAsync<ExamSection>(tenantId);
            await RemoveAsync<Exam>(tenantId);

            // The organisation's own vocabulary — its languages, its levels, its
            // competencies. It describes nothing once the organisation is gone.
            await RemoveAsync<Topic>(tenantId);
            await RemoveAsync<Level>(tenantId);
            await RemoveAsync<Category>(tenantId);
            await RemoveAsync<CategorySet>(tenantId);

            // The organisation's own staff, and what they signed in with.
            //
            // ABP removes the tenant row and nothing else, so these were left
            // behind as well: a name, an address, and a hash of a password
            // people reuse elsewhere. They are as personal as anything in the
            // exam tables, and "its people" in the dialog means the staff as
            // much as the candidates.
            await RemoveAsync<IdentityUser>(tenantId);
            await RemoveAsync<IdentityRole>(tenantId);

            // Grants describe roles that have gone, and settings hold the
            // organisation's name, its colour and the address of its logo.
            await RemoveAsync<PermissionGrant>(tenantId);
            await RemoveSettingsAsync(tenantId);

            await RemoveFilesAsync(files, tenantId);
        }
    }

    /// <summary>
    /// Every file this organisation put in storage: recorded answers, uploaded
    /// work, the pictures and recordings its questions are built on.
    /// </summary>
    private async Task<List<string>> FilesOfAsync(Guid tenantId)
    {
        var dataFilter = _services.GetRequiredService<IDataFilter>();

        using (dataFilter.Disable<IMultiTenant>())
        {
            var names = new List<string>();

            // Through the repository rather than a queryable projection: this
            // layer knows nothing about Entity Framework, and it must not learn.
            var answers = await _services.GetRequiredService<IRepository<Answer>>()
                .GetListAsync(a => a.TenantId == tenantId && a.AnswerBlobName != null);

            var questions = await _services.GetRequiredService<IRepository<Question>>()
                .GetListAsync(q => q.TenantId == tenantId && q.MediaBlobName != null);

            var groups = await _services.GetRequiredService<IRepository<QuestionGroup>>()
                .GetListAsync(g => g.TenantId == tenantId && g.StimulusBlobName != null);

            names.AddRange(answers.Select(a => a.AnswerBlobName!));
            names.AddRange(questions.Select(q => q.MediaBlobName!));
            names.AddRange(groups.Select(g => g.StimulusBlobName!));

            // And the organisation's own logo, whose address is a setting rather
            // than a column on anything.
            var key = tenantId.ToString();

            var settings = await _services.GetRequiredService<IRepository<Setting>>()
                .GetListAsync(setting =>
                    setting.ProviderName == TenantSettingProvider
                    && setting.ProviderKey == key
                    && setting.Name == Settings.InternshipManagementSystemSettings.LogoBlobName);

            names.AddRange(settings
                .Select(setting => setting.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return names.Distinct().ToList();
        }
    }

    /// <summary>
    /// The organisation's name, its colour, the address of its logo, and every
    /// other choice it made about itself.
    /// <para>
    /// Not matched on a tenant id, because ABP's settings are not multi-tenant
    /// entities: a tenant's value is a row whose provider is "T" and whose
    /// provider key is the tenant's id written out. Reaching for
    /// <c>TenantId</c> here compiles nowhere, which is the good kind of
    /// mistake — it says plainly that this table is keyed another way.
    /// </para>
    /// </summary>
    private async Task RemoveSettingsAsync(Guid tenantId)
    {
        var key = tenantId.ToString();
        var repository = _services.GetRequiredService<IRepository<Setting>>();

        await repository.DeleteAsync(
            setting => setting.ProviderName == TenantSettingProvider && setting.ProviderKey == key,
            autoSave: true);

        if (_unitOfWork.Current is { } current)
        {
            await current.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Removed the settings of deleted organisation {TenantId}", tenantId);
    }

    /// <summary>ABP's provider name for a value held by one tenant.</summary>
    private const string TenantSettingProvider = "T";

    /// <summary>
    /// The files, one by one, and never stopping on one that will not go.
    /// <para>
    /// A blob that has already vanished, or a storage backend that refuses, must
    /// not abort the rest: the alternative is a half-erased organisation whose
    /// remaining files nothing can find again, because the rows that named them
    /// are gone. Whatever is left over is reported by name so somebody can
    /// finish the job by hand.
    /// </para>
    /// </summary>
    private async Task RemoveFilesAsync(List<string> names, Guid tenantId)
    {
        foreach (var name in names)
        {
            try
            {
                await _blobs.DeleteAsync(name);
            }
            catch (Exception failure)
            {
                _logger.LogWarning(
                    failure,
                    "Could not remove {Blob} belonging to deleted organisation {TenantId}. "
                    + "It must be removed by hand: the row that named it has gone.",
                    name,
                    tenantId);
            }
        }

        _logger.LogInformation(
            "Removed {Count} stored files belonging to deleted organisation {TenantId}",
            names.Count,
            tenantId);
    }

    /// <summary>
    /// Everything of one kind belonging to that organisation.
    /// <para>
    /// Matched on the stored tenant id with the multi-tenant filter switched off,
    /// rather than trusting the ambient tenant. The filter is what normally keeps
    /// one organisation out of another's data, and this is the one operation that
    /// must be certain rather than merely filtered: a cascade that reached past
    /// its own tenant would take a paying customer's exams with a departing one's.
    /// </para>
    /// </summary>
    private async Task RemoveAsync<TEntity>(Guid tenantId)
        where TEntity : class, IEntity, IMultiTenant
    {
        var repository = _services.GetRequiredService<IRepository<TEntity>>();
        var dataFilter = _services.GetRequiredService<IDataFilter>();

        using (dataFilter.Disable<IMultiTenant>())
        {
            await repository.DeleteAsync(entity => entity.TenantId == tenantId, autoSave: true);
        }

        if (_unitOfWork.Current is { } current)
        {
            await current.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Removed {Entity} belonging to deleted organisation {TenantId}",
            typeof(TEntity).Name,
            tenantId);
    }
}
