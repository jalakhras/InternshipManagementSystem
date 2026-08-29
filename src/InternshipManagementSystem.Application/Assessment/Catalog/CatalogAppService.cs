using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Catalog;

/// <summary>
/// The organisation's own vocabulary: domains, their levels, their topics.
/// <para>
/// Everything here already existed as tables. What was missing was any way to put
/// a row in one, which is why <c>CategoryId</c> and <c>LevelId</c> were null on
/// every exam and every question in the product. That is not a cosmetic gap:
/// <c>Question.DrawableBy</c> collapses to "questions owned by this exam" when the
/// category is null, so the shared item bank was correct, tested, and unreachable.
/// </para>
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Catalog.View)]
public class CatalogAppService : ApplicationService, ICatalogAppService
{
    private readonly IRepository<Category, Guid> _categories;
    private readonly IRepository<Level, Guid> _levels;
    private readonly IRepository<Topic, Guid> _topics;
    private readonly IRepository<CategorySet, Guid> _vocabulary;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<Question, Guid> _questions;

    public CatalogAppService(
        IRepository<Category, Guid> categories,
        IRepository<Level, Guid> levels,
        IRepository<Topic, Guid> topics,
        IRepository<CategorySet, Guid> vocabulary,
        IRepository<Exam, Guid> exams,
        IRepository<Question, Guid> questions)
    {
        _categories = categories;
        _levels = levels;
        _topics = topics;
        _vocabulary = vocabulary;
        _exams = exams;
        _questions = questions;
    }

    // ------------------------------------------------------------- categories

    public async Task<List<CategoryDto>> GetCategoriesAsync(bool includeInactive = false)
    {
        var categories = await (await _categories.GetQueryableAsync())
            .WhereIf(!includeInactive, c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var levels = await (await _levels.GetQueryableAsync())
            .WhereIf(!includeInactive, l => l.IsActive)
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToListAsync();

        var topics = await (await _topics.GetQueryableAsync())
            .WhereIf(!includeInactive, t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        // Counted in one pass each rather than per category. A catalogue is small;
        // the number of exams under it is not necessarily.
        var examCounts = await (await _exams.GetQueryableAsync())
            .Where(e => e.CategoryId != null)
            .GroupBy(e => e.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync();

        var questionCounts = await (await _questions.GetQueryableAsync())
            .Where(q => q.CategoryId != null)
            .GroupBy(q => q.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync();

        var exams = examCounts.ToDictionary(x => x.CategoryId, x => x.Count);
        var questions = questionCounts.ToDictionary(x => x.CategoryId, x => x.Count);

        return categories.Select(category => new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,

            // A level or topic with no category belongs to all of them. An
            // organisation whose ladder is the same across subjects should write it
            // once rather than once per subject.
            Levels = levels
                .Where(l => l.CategoryId == null || l.CategoryId == category.Id)
                .Select(ToDto)
                .ToList(),
            Topics = topics
                .Where(t => t.CategoryId == null || t.CategoryId == category.Id)
                .Select(ToDto)
                .ToList(),

            ExamCount = exams.GetValueOrDefault(category.Id),
            QuestionCount = questions.GetValueOrDefault(category.Id),
        }).ToList();
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<CategoryDto> CreateCategoryAsync(CreateUpdateCategoryDto input)
    {
        await EnsureCategoryCodeIsFreeAsync(input.Code, null);

        var category = await _categories.InsertAsync(
            new Category(GuidGenerator.Create(), CurrentTenant.Id, input.Code.Trim(), input.Name.Trim())
            {
                Description = input.Description,
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive,
            },
            autoSave: true);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
        };
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, CreateUpdateCategoryDto input)
    {
        var category = await _categories.GetAsync(id);

        await EnsureCategoryCodeIsFreeAsync(input.Code, id);

        category.Name = input.Name.Trim();
        category.Code = input.Code.Trim();
        category.Description = input.Description;
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;

        await _categories.UpdateAsync(category, autoSave: true);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
        };
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task DeleteCategoryAsync(Guid id)
    {
        // Refused rather than cascaded. Deleting a domain that exams are filed
        // under would unfile them, and an unfiled exam draws from an empty bank —
        // a paper that silently gets shorter, which is the worst way for this to
        // fail. Deactivating does the useful half without the damage.
        if (await (await _exams.GetQueryableAsync()).AnyAsync(e => e.CategoryId == id)
            || await (await _questions.GetQueryableAsync()).AnyAsync(q => q.CategoryId == id))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogCategoryInUse);
        }

        // The levels and topics under it go with it. They describe this domain and
        // mean nothing without it.
        var levels = await (await _levels.GetQueryableAsync()).Where(l => l.CategoryId == id).ToListAsync();
        var topics = await (await _topics.GetQueryableAsync()).Where(t => t.CategoryId == id).ToListAsync();

        await _levels.DeleteManyAsync(levels, autoSave: true);
        await _topics.DeleteManyAsync(topics, autoSave: true);
        await _categories.DeleteAsync(id, autoSave: true);
    }

    // ----------------------------------------------------------------- levels

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<LevelDto> CreateLevelAsync(CreateUpdateLevelDto input)
    {
        await EnsureLevelCodeIsFreeAsync(input.Code, null);

        var level = await _levels.InsertAsync(
            new Level(GuidGenerator.Create(), CurrentTenant.Id, input.Code.Trim(), input.Name.Trim(), input.DisplayOrder)
            {
                CategoryId = input.CategoryId,
                IsActive = input.IsActive,
            },
            autoSave: true);

        return ToDto(level);
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<LevelDto> UpdateLevelAsync(Guid id, CreateUpdateLevelDto input)
    {
        var level = await _levels.GetAsync(id);

        await EnsureLevelCodeIsFreeAsync(input.Code, id);

        level.CategoryId = input.CategoryId;
        level.Name = input.Name.Trim();
        level.Code = input.Code.Trim();
        level.DisplayOrder = input.DisplayOrder;
        level.IsActive = input.IsActive;

        await _levels.UpdateAsync(level, autoSave: true);

        return ToDto(level);
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task DeleteLevelAsync(Guid id)
    {
        if (await (await _exams.GetQueryableAsync()).AnyAsync(e => e.LevelId == id)
            || await (await _questions.GetQueryableAsync()).AnyAsync(q => q.LevelId == id))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogLevelInUse);
        }

        await _levels.DeleteAsync(id, autoSave: true);
    }

    // ----------------------------------------------------------------- topics

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<TopicDto> CreateTopicAsync(CreateUpdateTopicDto input)
    {
        await EnsureTopicCodeIsFreeAsync(input.Code, null);

        var topic = await _topics.InsertAsync(
            new Topic(GuidGenerator.Create(), CurrentTenant.Id, input.Code.Trim(), input.Name.Trim(), input.ParentId)
            {
                CategoryId = input.CategoryId,
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive,
            },
            autoSave: true);

        return ToDto(topic);
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<TopicDto> UpdateTopicAsync(Guid id, CreateUpdateTopicDto input)
    {
        var topic = await _topics.GetAsync(id);

        await EnsureTopicCodeIsFreeAsync(input.Code, id);

        // A topic that is its own ancestor makes the breakdown on a result loop
        // forever. Cheap to check here, impossible to recover from later.
        if (input.ParentId == id || await WouldCycleAsync(id, input.ParentId))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogTopicCycle);
        }

        topic.CategoryId = input.CategoryId;
        topic.Name = input.Name.Trim();
        topic.Code = input.Code.Trim();
        topic.ParentId = input.ParentId;
        topic.DisplayOrder = input.DisplayOrder;
        topic.IsActive = input.IsActive;

        await _topics.UpdateAsync(topic, autoSave: true);

        return ToDto(topic);
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task DeleteTopicAsync(Guid id)
    {
        if (await (await _questions.GetQueryableAsync()).AnyAsync(q => q.TopicId == id))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogTopicInUse);
        }

        // Children are promoted rather than deleted. Removing "grammar" should not
        // take "past perfect" with it — the questions filed under the child are
        // still about something.
        var children = await (await _topics.GetQueryableAsync()).Where(t => t.ParentId == id).ToListAsync();
        var topic = await _topics.GetAsync(id);

        foreach (var child in children)
        {
            child.ParentId = topic.ParentId;
        }

        await _topics.UpdateManyAsync(children, autoSave: true);
        await _topics.DeleteAsync(id, autoSave: true);
    }

    // ------------------------------------------------------------- vocabulary

    public async Task<CategorySetDto> GetVocabularyAsync()
    {
        var set = await (await _vocabulary.GetQueryableAsync()).FirstOrDefaultAsync();

        // Defaults rather than null. A tenant that never opened this screen still
        // needs words on every other one.
        return set is null
            ? new CategorySetDto
            {
                SingularName = "Category",
                PluralName = "Categories",
                SubjectSingularName = "Candidate",
                SubjectPluralName = "Candidates",
                GroupSingularName = "Group",
                GroupPluralName = "Groups",
            }
            : ToDto(set);
    }

    [Authorize(InternshipManagementSystemPermissions.Catalog.Manage)]
    public async Task<CategorySetDto> UpdateVocabularyAsync(UpdateCategorySetDto input)
    {
        var set = await (await _vocabulary.GetQueryableAsync()).FirstOrDefaultAsync();

        if (set is null)
        {
            set = await _vocabulary.InsertAsync(
                new CategorySet(
                    GuidGenerator.Create(), CurrentTenant.Id,
                    input.SingularName.Trim(), input.PluralName.Trim()),
                autoSave: true);
        }
        else
        {
            set.SingularName = input.SingularName.Trim();
            set.PluralName = input.PluralName.Trim();
        }

        set.SubjectSingularName = input.SubjectSingularName.Trim();
        set.SubjectPluralName = input.SubjectPluralName.Trim();
        set.GroupSingularName = input.GroupSingularName.Trim();
        set.GroupPluralName = input.GroupPluralName.Trim();

        await _vocabulary.UpdateAsync(set, autoSave: true);

        return ToDto(set);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Whether making <paramref name="parentId"/> the parent of <paramref name="id"/>
    /// would close a loop, by walking up from the proposed parent.
    /// </summary>
    private async Task<bool> WouldCycleAsync(Guid id, Guid? parentId)
    {
        var topics = await (await _topics.GetQueryableAsync())
            .Select(t => new { t.Id, t.ParentId })
            .ToListAsync();

        var parents = topics.ToDictionary(t => t.Id, t => t.ParentId);
        var walker = parentId;

        // Bounded by the number of topics, so a loop already in the data cannot
        // hang this the way a plain while would.
        for (var step = 0; step < topics.Count && walker is not null; step++)
        {
            if (walker == id)
            {
                return true;
            }

            walker = parents.GetValueOrDefault(walker.Value);
        }

        return false;
    }

    private async Task EnsureCategoryCodeIsFreeAsync(string code, Guid? exceptId)
    {
        var trimmed = code.Trim();

        if (await (await _categories.GetQueryableAsync())
                .AnyAsync(c => c.Code == trimmed && (exceptId == null || c.Id != exceptId)))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogCodeAlreadyExists)
                .WithData("Code", trimmed);
        }
    }

    private async Task EnsureLevelCodeIsFreeAsync(string code, Guid? exceptId)
    {
        var trimmed = code.Trim();

        if (await (await _levels.GetQueryableAsync())
                .AnyAsync(l => l.Code == trimmed && (exceptId == null || l.Id != exceptId)))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogCodeAlreadyExists)
                .WithData("Code", trimmed);
        }
    }

    private async Task EnsureTopicCodeIsFreeAsync(string code, Guid? exceptId)
    {
        var trimmed = code.Trim();

        if (await (await _topics.GetQueryableAsync())
                .AnyAsync(t => t.Code == trimmed && (exceptId == null || t.Id != exceptId)))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.CatalogCodeAlreadyExists)
                .WithData("Code", trimmed);
        }
    }

    private static LevelDto ToDto(Level level) => new()
    {
        Id = level.Id,
        CategoryId = level.CategoryId,
        Name = level.Name,
        Code = level.Code,
        DisplayOrder = level.DisplayOrder,
        IsActive = level.IsActive,
    };

    private static TopicDto ToDto(Topic topic) => new()
    {
        Id = topic.Id,
        CategoryId = topic.CategoryId,
        Name = topic.Name,
        Code = topic.Code,
        ParentId = topic.ParentId,
        DisplayOrder = topic.DisplayOrder,
        IsActive = topic.IsActive,
    };

    private static CategorySetDto ToDto(CategorySet set) => new()
    {
        SingularName = set.SingularName,
        PluralName = set.PluralName,
        SubjectSingularName = set.SubjectSingularName,
        SubjectPluralName = set.SubjectPluralName,
        GroupSingularName = set.GroupSingularName,
        GroupPluralName = set.GroupPluralName,
    };
}
