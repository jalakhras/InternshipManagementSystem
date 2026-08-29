using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// The shape of an exam: what parts it has, and which fixed papers exist.
/// <para>
/// Separate from <see cref="IExamAppService"/> because these are decisions about
/// structure rather than about an exam's settings, and they are made by
/// different people at different times — a coordinator sets the pass mark, and
/// whoever owns the syllabus decides there are four skills.
/// </para>
/// </summary>
public interface IExamStructureAppService : IApplicationService
{
    // ------------------------------------------------------------- sections

    Task<List<ExamSectionDto>> GetSectionsAsync(Guid examId);

    Task<ExamSectionDto> CreateSectionAsync(CreateUpdateExamSectionDto input);

    Task<ExamSectionDto> UpdateSectionAsync(Guid id, CreateUpdateExamSectionDto input);

    Task DeleteSectionAsync(Guid id);

    // ---------------------------------------------------------------- forms

    Task<List<ExamFormDto>> GetFormsAsync(Guid examId);

    Task<ExamFormDetailDto> GetFormAsync(Guid id);

    Task<ExamFormDto> CreateFormAsync(CreateUpdateExamFormDto input);

    /// <summary>
    /// Fills a draft form from the exam's blueprint.
    /// <para>
    /// So an author reviews a paper rather than assembling one. What comes back
    /// is a draft either way: nothing is fixed until it is published.
    /// </para>
    /// </summary>
    Task<ExamFormDetailDto> GenerateFormAsync(Guid id, GenerateExamFormDto input);

    Task<ExamFormDetailDto> SetFormQuestionsAsync(Guid id, SetExamFormQuestionsDto input);

    /// <summary>
    /// Freezes a form for use. After this its questions do not change, because two
    /// candidates who sat "Form 2" must have answered the same paper.
    /// </summary>
    Task<ExamFormDto> PublishFormAsync(Guid id);

    /// <summary>
    /// Takes a form out of rotation without deleting it, so results that reference
    /// it keep resolving.
    /// </summary>
    Task<ExamFormDto> RetireFormAsync(Guid id);

    Task DeleteFormAsync(Guid id);
}
