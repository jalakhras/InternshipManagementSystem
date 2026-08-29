using System;
using System.Collections.Generic;
using System.Text;
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

    /// <summary>
    /// The example spreadsheet, generated rather than written by hand.
    /// <para>
    /// Declared before the <c>{id}</c> route so "import" is not read as an id.
    /// Fetched by the screen and saved as a file rather than linked: a plain
    /// anchor to this path resolves against the application rather than the API,
    /// and carries no token even when it does not — which is how the results
    /// export once navigated a coordinator to the dashboard.
    /// </para>
    /// </summary>
    [HttpGet("import/template")]
    public async Task<IActionResult> GetImportTemplateAsync()
    {
        var csv = await _questions.GetImportTemplateAsync();

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", "questions-template.csv");
    }

    /// <summary>
    /// Reads a spreadsheet of questions. With <c>dryRun</c> it writes nothing.
    /// </summary>
    [HttpPost("import")]
    public Task<ImportQuestionsResultDto> ImportAsync([FromBody] ImportQuestionsDto input) =>
        _questions.ImportAsync(input);

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
