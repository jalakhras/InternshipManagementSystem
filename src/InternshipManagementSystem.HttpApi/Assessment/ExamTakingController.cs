using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// The endpoints a person sitting an exam calls.
/// <para>
/// Anonymous by design: takers have no account. Authorisation is the exam-session
/// credential in the <c>X-Exam-Session</c> header, which names one attempt and
/// nothing else, so these routes cannot reach staff data even if called directly.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/take")]
[AllowAnonymous]
public class ExamTakingController : AbpControllerBase
{
    private const string SessionHeader = "X-Exam-Session";

    private readonly IExamTakingAppService _service;

    public ExamTakingController(IExamTakingAppService service)
    {
        _service = service;
    }

    /// <summary>Opens a link. Does not consume an attempt.</summary>
    [HttpGet("{token}")]
    public Task<ExamPreviewDto> OpenAsync(string token) => _service.OpenLinkAsync(token);

    /// <summary>Starts or resumes. This is where the clock begins.</summary>
    [HttpPost("start")]
    public Task<AttemptStateDto> StartAsync() => _service.StartAsync(Session);

    [HttpGet("state")]
    public Task<AttemptStateDto> GetStateAsync() => _service.GetStateAsync(Session);

    /// <summary>
    /// One question at a time. The whole paper is never in the browser, so a taker
    /// with developer tools open still sees only the question in front of them.
    /// </summary>
    [HttpGet("question/{position:int}")]
    public Task<TakerQuestionDto> GetQuestionAsync(int position) => _service.GetQuestionAsync(Session, position);

    /// <summary>Autosave. The response carries the authoritative remaining time.</summary>
    [HttpPut("answer")]
    public Task<SaveAnswerResultDto> SaveAnswerAsync([FromBody] SaveAnswerDto input) =>
        _service.SaveAnswerAsync(Session, input);

    [HttpPost("signal")]
    public Task ReportSignalAsync([FromBody] ReportIntegritySignalDto input) =>
        _service.ReportSignalAsync(Session, input);

    [HttpPost("submit")]
    public Task<AttemptResultDto> SubmitAsync() => _service.SubmitAsync(Session);

    [HttpGet("result")]
    public Task<AttemptResultDto> GetResultAsync() => _service.GetResultAsync(Session);

    private string Session =>
        Request.Headers.TryGetValue(SessionHeader, out var value)
            ? value.ToString()
            : string.Empty;
}
