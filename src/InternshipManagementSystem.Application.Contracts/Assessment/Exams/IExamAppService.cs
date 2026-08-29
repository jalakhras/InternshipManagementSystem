using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>Authoring exams and the rules that turn a bank into one taker's paper.</summary>
public interface IExamAppService : IApplicationService
{
    Task<PagedResultDto<ExamDto>> GetListAsync(ExamListRequestDto input);

    Task<ExamDto> GetAsync(Guid id);

    Task<ExamDto> CreateAsync(CreateUpdateExamDto input);

    Task<ExamDto> UpdateAsync(Guid id, CreateUpdateExamDto input);

    Task DeleteAsync(Guid id);

    /// <summary>
    /// Everything that would prevent publishing, plus warnings that would not.
    /// Returned in one pass so the author is not led through a sequence of refusals.
    /// </summary>
    Task<PublishCheckDto> CheckPublishAsync(Guid id);

    /// <summary>Moves a draft to assignable. Refuses if any blocker stands.</summary>
    Task<ExamDto> PublishAsync(Guid id);

    /// <summary>Stops new assignments. Attempts under way finish normally.</summary>
    Task<ExamDto> ArchiveAsync(Guid id);

    /// <summary>
    /// The form recipe, with how many bank questions actually match each rule — so a
    /// rule that cannot be filled is visible before a candidate's paper is short.
    /// </summary>
    Task<List<BlueprintRuleDto>> GetBlueprintAsync(Guid examId);

    /// <summary>Replaces the blueprint wholesale. It is one recipe, not a set of rows.</summary>
    Task<List<BlueprintRuleDto>> SetBlueprintAsync(Guid examId, List<CreateUpdateBlueprintRuleDto> rules);
}
