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

    /// <summary>Shared stimuli and the questions under each.</summary>
    Task<List<QuestionGroupDto>> GetGroupsAsync(Guid examId);

    Task<QuestionGroupDto> CreateGroupAsync(CreateUpdateQuestionGroupDto input);
}
