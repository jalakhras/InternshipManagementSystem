using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Catalog.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// The organisation's domains, levels, topics and its own words for them.
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/catalog")]
public class CatalogController : AbpControllerBase
{
    private readonly ICatalogAppService _catalog;

    public CatalogController(ICatalogAppService catalog)
    {
        _catalog = catalog;
    }

    /// <summary>Everything at once: a catalogue is small and every caller wants all of it.</summary>
    [HttpGet("categories")]
    public Task<List<CategoryDto>> GetCategoriesAsync([FromQuery] bool includeInactive = false) =>
        _catalog.GetCategoriesAsync(includeInactive);

    [HttpPost("categories")]
    public Task<CategoryDto> CreateCategoryAsync([FromBody] CreateUpdateCategoryDto input) =>
        _catalog.CreateCategoryAsync(input);

    [HttpPut("categories/{id}")]
    public Task<CategoryDto> UpdateCategoryAsync(Guid id, [FromBody] CreateUpdateCategoryDto input) =>
        _catalog.UpdateCategoryAsync(id, input);

    [HttpDelete("categories/{id}")]
    public Task DeleteCategoryAsync(Guid id) => _catalog.DeleteCategoryAsync(id);

    [HttpPost("levels")]
    public Task<LevelDto> CreateLevelAsync([FromBody] CreateUpdateLevelDto input) =>
        _catalog.CreateLevelAsync(input);

    [HttpPut("levels/{id}")]
    public Task<LevelDto> UpdateLevelAsync(Guid id, [FromBody] CreateUpdateLevelDto input) =>
        _catalog.UpdateLevelAsync(id, input);

    [HttpDelete("levels/{id}")]
    public Task DeleteLevelAsync(Guid id) => _catalog.DeleteLevelAsync(id);

    [HttpPost("topics")]
    public Task<TopicDto> CreateTopicAsync([FromBody] CreateUpdateTopicDto input) =>
        _catalog.CreateTopicAsync(input);

    [HttpPut("topics/{id}")]
    public Task<TopicDto> UpdateTopicAsync(Guid id, [FromBody] CreateUpdateTopicDto input) =>
        _catalog.UpdateTopicAsync(id, input);

    [HttpDelete("topics/{id}")]
    public Task DeleteTopicAsync(Guid id) => _catalog.DeleteTopicAsync(id);

    [HttpGet("vocabulary")]
    public Task<CategorySetDto> GetVocabularyAsync() => _catalog.GetVocabularyAsync();

    [HttpPut("vocabulary")]
    public Task<CategorySetDto> UpdateVocabularyAsync([FromBody] UpdateCategorySetDto input) =>
        _catalog.UpdateVocabularyAsync(input);
}
