using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Catalog;

/// <summary>
/// The vocabulary an organisation tests in: its domains, their levels, their topics.
/// <para>
/// This existed in the schema and nowhere else — no service, no route, no screen,
/// no seed. Which meant every exam and every question was filed under no domain and
/// no level, and three separate features quietly stopped working: the shared item
/// bank (whose whole drawing rule is domain plus level), the blueprint (which draws
/// per topic), and the topic breakdown on a result, which was always empty because
/// there were no topics to break down by.
/// </para>
/// <para>
/// So this is not a settings screen. It is the thing the rest of the product files
/// against.
/// </para>
/// </summary>
public interface ICatalogAppService : IApplicationService
{
    /// <summary>
    /// Every domain with its levels and topics, in display order.
    /// <para>
    /// One call rather than three, because everything that needs a category needs
    /// its levels in the same breath — the exam form, the question form, the
    /// blueprint editor — and a catalogue is small enough that paging it would cost
    /// more than it saves.
    /// </para>
    /// </summary>
    Task<List<CategoryDto>> GetCategoriesAsync(bool includeInactive = false);

    Task<CategoryDto> CreateCategoryAsync(CreateUpdateCategoryDto input);

    Task<CategoryDto> UpdateCategoryAsync(Guid id, CreateUpdateCategoryDto input);

    /// <summary>
    /// Removes a domain nothing points at.
    /// <para>
    /// Refused once an exam or a question is filed under it, because deleting it
    /// would leave those unfiled and silently take them out of the bank. Deactivate
    /// instead: it disappears from the pickers and everything already filed keeps
    /// meaning what it meant.
    /// </para>
    /// </summary>
    Task DeleteCategoryAsync(Guid id);

    Task<LevelDto> CreateLevelAsync(CreateUpdateLevelDto input);

    Task<LevelDto> UpdateLevelAsync(Guid id, CreateUpdateLevelDto input);

    Task DeleteLevelAsync(Guid id);

    Task<TopicDto> CreateTopicAsync(CreateUpdateTopicDto input);

    Task<TopicDto> UpdateTopicAsync(Guid id, CreateUpdateTopicDto input);

    Task DeleteTopicAsync(Guid id);

    /// <summary>The tenant's own words. Never null: unset falls back to the defaults.</summary>
    Task<CategorySetDto> GetVocabularyAsync();

    Task<CategorySetDto> UpdateVocabularyAsync(UpdateCategorySetDto input);
}
