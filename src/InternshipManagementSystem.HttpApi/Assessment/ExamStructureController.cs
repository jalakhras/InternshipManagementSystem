using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// An exam's sections and its named forms.
/// <para>
/// A form's contents carry the answer key by association — knowing which
/// questions are on Form 2 is most of the way to knowing what will be asked —
/// so every route here sits behind the exam permissions, reads included.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/exam-structure")]
public class ExamStructureController : AbpControllerBase
{
    private readonly IExamStructureAppService _structure;

    public ExamStructureController(IExamStructureAppService structure)
    {
        _structure = structure;
    }

    // --------------------------------------------------------------- sections

    [HttpGet("sections/{examId}")]
    public Task<List<ExamSectionDto>> GetSectionsAsync(Guid examId) => _structure.GetSectionsAsync(examId);

    [HttpPost("sections")]
    public Task<ExamSectionDto> CreateSectionAsync([FromBody] CreateUpdateExamSectionDto input) =>
        _structure.CreateSectionAsync(input);

    [HttpPut("sections/{id}")]
    public Task<ExamSectionDto> UpdateSectionAsync(Guid id, [FromBody] CreateUpdateExamSectionDto input) =>
        _structure.UpdateSectionAsync(id, input);

    [HttpDelete("sections/{id}")]
    public Task DeleteSectionAsync(Guid id) => _structure.DeleteSectionAsync(id);

    // ------------------------------------------------------------------ forms

    [HttpGet("forms/by-exam/{examId}")]
    public Task<List<ExamFormDto>> GetFormsAsync(Guid examId) => _structure.GetFormsAsync(examId);

    /// <summary>Declared after the by-exam route so "by-exam" is not read as an id.</summary>
    [HttpGet("forms/{id}")]
    public Task<ExamFormDetailDto> GetFormAsync(Guid id) => _structure.GetFormAsync(id);

    [HttpPost("forms")]
    public Task<ExamFormDto> CreateFormAsync([FromBody] CreateUpdateExamFormDto input) =>
        _structure.CreateFormAsync(input);

    [HttpPost("forms/{id}/generate")]
    public Task<ExamFormDetailDto> GenerateFormAsync(Guid id, [FromBody] GenerateExamFormDto input) =>
        _structure.GenerateFormAsync(id, input);

    [HttpPut("forms/{id}/questions")]
    public Task<ExamFormDetailDto> SetFormQuestionsAsync(Guid id, [FromBody] SetExamFormQuestionsDto input) =>
        _structure.SetFormQuestionsAsync(id, input);

    [HttpPost("forms/{id}/publish")]
    public Task<ExamFormDto> PublishFormAsync(Guid id) => _structure.PublishFormAsync(id);

    [HttpPost("forms/{id}/retire")]
    public Task<ExamFormDto> RetireFormAsync(Guid id) => _structure.RetireFormAsync(id);

    [HttpDelete("forms/{id}")]
    public Task DeleteFormAsync(Guid id) => _structure.DeleteFormAsync(id);
}
