using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Results.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Results;

/// <summary>
/// What happened when people sat the exam.
/// <para>
/// The permissions for this existed — <c>Results.View</c>, <c>.Export</c>,
/// <c>.ViewItemAnalysis</c> — and behind them was nothing: no service, no
/// controller, no screen. A centre could write an exam, send it to forty
/// students, have every paper marked automatically, and then have no way to see
/// a single score. The review queue does not fill the gap: it lists only sittings
/// that need a person to mark them, so an all-multiple-choice paper — the common
/// case — appeared nowhere at all.
/// </para>
/// </summary>
public interface IResultAppService : IApplicationService
{
    /// <summary>One row per sitting, filtered and paged.</summary>
    Task<PagedResultDto<ResultRowDto>> GetListAsync(ResultListRequestDto input);

    /// <summary>
    /// The figures above the roster, over the whole filtered set rather than the
    /// current page.
    /// </summary>
    Task<ResultSummaryDto> GetSummaryAsync(ResultListRequestDto input);

    /// <summary>One sitting in full, question by question, with a topic breakdown.</summary>
    Task<ResultDetailDto> GetAsync(Guid attemptId);

    /// <summary>
    /// The same rows as the list, as CSV.
    /// <para>
    /// Because the next thing that happens to a set of results is that somebody
    /// puts them in a spreadsheet, and if the product cannot produce one they will
    /// retype them.
    /// </para>
    /// </summary>
    Task<string> ExportCsvAsync(ResultListRequestDto input);

    /// <summary>
    /// How each question in an exam has behaved across every attempt.
    /// </summary>
    Task<List<ItemAnalysisRowDto>> GetItemAnalysisAsync(Guid examId);
}
