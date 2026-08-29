using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// The question bank.
/// <para>
/// Every route here returns the answer key, which is why the whole controller
/// sits behind <c>Questions.*</c> — including the reads. What a candidate receives
/// comes from <c>ExamTakingController</c> and shares no type with any of these.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/questions")]
public class QuestionController : AbpControllerBase
{
    private readonly IQuestionAppService _questions;

    public QuestionController(IQuestionAppService questions)
    {
        _questions = questions;
    }

    /// <summary>
    /// The types this server supports.
    /// <para>
    /// Declared before the <c>{id}</c> route so "types" is not read as an id.
    /// </para>
    /// </summary>
    [HttpGet("types")]
    public Task<List<QuestionTypeDescriptorDto>> GetTypesAsync() => _questions.GetTypesAsync();

    [HttpGet]
    public Task<PagedResultDto<QuestionDto>> GetListAsync([FromQuery] QuestionListRequestDto input) =>
        _questions.GetListAsync(input);

    [HttpGet("{id}")]
    public Task<QuestionDto> GetAsync(Guid id) => _questions.GetAsync(id);

    [HttpPost]
    public Task<QuestionDto> CreateAsync([FromBody] CreateUpdateQuestionDto input) =>
        _questions.CreateAsync(input);

    [HttpPut("{id}")]
    public Task<QuestionDto> UpdateAsync(Guid id, [FromBody] CreateUpdateQuestionDto input) =>
        _questions.UpdateAsync(id, input);

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => _questions.DeleteAsync(id);

    /// <summary>
    /// Checks a payload without saving it, so the authoring form can warn while
    /// someone is still typing rather than at the moment they press save.
    /// </summary>
    [HttpPost("validate-payload")]
    public Task<List<string>> ValidatePayloadAsync([FromBody] ValidatePayloadRequestDto input) =>
        _questions.ValidatePayloadAsync(input.Type, input.Payload);

    [HttpGet("groups/{examId}")]
    public Task<List<QuestionGroupDto>> GetGroupsAsync(Guid examId) => _questions.GetGroupsAsync(examId);

    [HttpPut("groups/{id}")]
    public Task<QuestionGroupDto> UpdateGroupAsync(Guid id, [FromBody] CreateUpdateQuestionGroupDto input) =>
        _questions.UpdateGroupAsync(id, input);

    [HttpDelete("groups/{id}")]
    public Task DeleteGroupAsync(Guid id) => _questions.DeleteGroupAsync(id);

    [HttpPost("groups")]
    public Task<QuestionGroupDto> CreateGroupAsync([FromBody] CreateUpdateQuestionGroupDto input) =>
        _questions.CreateGroupAsync(input);
}

/// <summary>Body for the payload check. A record because it carries nothing but data.</summary>
public class ValidatePayloadRequestDto
{
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = "{}";
}
