using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// The question bank — the authoring side, where the answer keys live.
/// <para>
/// Every method here is behind <c>Questions.*</c>. What a candidate receives comes
/// from <c>IExamTakingAppService</c> and shares no type with anything on this
/// interface, which is why the key cannot leak by someone reusing a DTO.
/// </para>
/// </summary>
public interface IQuestionAppService : IApplicationService
{
    Task<PagedResultDto<QuestionDto>> GetListAsync(QuestionListRequestDto input);

    Task<QuestionDto> GetAsync(Guid id);

    /// <summary>Refuses a payload the grader could not read. See QuestionPayloadValidator.</summary>
    Task<QuestionDto> CreateAsync(CreateUpdateQuestionDto input);

    Task<QuestionDto> UpdateAsync(Guid id, CreateUpdateQuestionDto input);

    Task DeleteAsync(Guid id);

    /// <summary>
    /// Advice on a payload without saving it, so the authoring form can warn while
    /// someone is still typing rather than at the moment they press save.
    /// </summary>
    Task<List<string>> ValidatePayloadAsync(string type, string payload);

    /// <summary>
    /// The types this server supports, so the authoring UI and the graders cannot
    /// disagree about what exists.
    /// </summary>
    Task<List<QuestionTypeDescriptorDto>> GetTypesAsync();

    /// <summary>
    /// Reads a spreadsheet of questions and writes whichever rows are usable.
    /// <para>
    /// With <c>DryRun</c> it reports what it read and writes nothing, so an
    /// author importing eighty rows sees the four that are wrong while the
    /// spreadsheet is still open in front of them.
    /// </para>
    /// <para>
    /// One bad row never costs the good ones. Only a file that cannot be read at
    /// all — no headings, no question column — is refused outright, and it is
    /// refused before a single question is written.
    /// </para>
    /// </summary>
    Task<ImportQuestionsResultDto> ImportAsync(ImportQuestionsDto input);

    /// <summary>
    /// The example spreadsheet, generated rather than documented.
    /// <para>
    /// The headings come from the same constants the parser matches against, so
    /// the file an author downloads is by construction a file this server can
    /// read. A page telling somebody which headings to type by hand is a page
    /// that goes stale and takes the import with it.
    /// </para>
    /// </summary>
    Task<string> GetImportTemplateAsync();

    /// <summary>Shared stimuli and the questions under each.</summary>
    Task<List<QuestionGroupDto>> GetGroupsAsync(Guid examId);

    Task<QuestionGroupDto> CreateGroupAsync(CreateUpdateQuestionGroupDto input);

    /// <summary>
    /// Corrects a stimulus in place.
    /// <para>
    /// A reading passage is several hundred words somebody typed once. Without
    /// this, a typo in it is permanent and the only remedy is a new passage and
    /// six questions moved onto it by hand.
    /// </para>
    /// </summary>
    Task<QuestionGroupDto> UpdateGroupAsync(Guid id, CreateUpdateQuestionGroupDto input);

    /// <summary>
    /// Removes a stimulus. The questions under it survive as loose questions.
    /// <para>
    /// Deleting six questions because the passage above them was wrong is the
    /// kind of loss that makes people stop trusting a delete button.
    /// </para>
    /// </summary>
    Task DeleteGroupAsync(Guid id);
}
