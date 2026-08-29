using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// The taker's whole journey: open a link, start, answer, submit, see the result.
/// <para>
/// Every method after <see cref="OpenLinkAsync"/> is authorised by the exam-session
/// credential that method returns, not by a staff permission. That separation is
/// what makes anonymous access work at all — see <c>ExamSessionTokenService</c>.
/// </para>
/// </summary>
public interface IExamTakingAppService : IApplicationService
{
    /// <summary>
    /// Exchanges a link token for a preview and, when the link is usable, a session
    /// credential. Does not consume an attempt: looking is not sitting.
    /// </summary>
    Task<ExamPreviewDto> OpenLinkAsync(string token);

    /// <summary>
    /// Starts the attempt, or resumes the one already in progress. This is the point
    /// at which the link's attempt count moves and the clock begins.
    /// </summary>
    Task<AttemptStateDto> StartAsync(string sessionToken);

    /// <summary>Fetches one question of the taker's frozen form by position.</summary>
    Task<TakerQuestionDto> GetQuestionAsync(string sessionToken, int position);

    /// <summary>Saves one answer. Returns the authoritative remaining time.</summary>
    Task<SaveAnswerResultDto> SaveAnswerAsync(string sessionToken, SaveAnswerDto input);

    /// <summary>Current progress and remaining time, for the question map and the timer.</summary>
    Task<AttemptStateDto> GetStateAsync(string sessionToken);

    /// <summary>Records a behavioural observation. Advisory, for a human reviewer.</summary>
    Task ReportSignalAsync(string sessionToken, ReportIntegritySignalDto input);

    /// <summary>Submits the attempt and triggers grading.</summary>
    Task<AttemptResultDto> SubmitAsync(string sessionToken);

    /// <summary>
    /// The result. Withheld while a human still has answers to mark, and stripped of
    /// correct answers unless the exam is in Practice mode.
    /// </summary>
    Task<AttemptResultDto> GetResultAsync(string sessionToken);
}
