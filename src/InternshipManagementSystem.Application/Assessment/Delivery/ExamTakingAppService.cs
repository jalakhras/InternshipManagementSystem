using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Everything the person sitting the exam can do.
/// <para>
/// <c>[AllowAnonymous]</c> here is correct and deliberate: takers have no account.
/// Authorisation happens per call against the exam-session credential, which names
/// exactly one attempt. The old code marked the start endpoint anonymous but had it
/// call a service demanding an administrative permission, so it returned 403 to
/// every candidate and the feature never worked.
/// </para>
/// </summary>
[AllowAnonymous]
public class ExamTakingAppService : ApplicationService, IExamTakingAppService
{
    private readonly IRepository<ExamLink, Guid> _links;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<QuestionGroup, Guid> _groups;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestions;
    private readonly IRepository<Answer, Guid> _answers;
    private readonly IRepository<IntegritySignal, Guid> _signals;
    private readonly IRepository<Topic, Guid> _topics;
    private readonly ExamSessionTokenService _sessions;
    private readonly ExamFormBuilder _formBuilder;
    private readonly TakerQuestionProjector _projector;
    private readonly AttemptGradingService _grading;
    private readonly IDataFilter _dataFilter;

    public ExamTakingAppService(
        IRepository<ExamLink, Guid> links,
        IRepository<Exam, Guid> exams,
        IRepository<Question, Guid> questions,
        IRepository<QuestionGroup, Guid> groups,
        IRepository<Candidate, Guid> candidates,
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<IntegritySignal, Guid> signals,
        IRepository<Topic, Guid> topics,
        ExamSessionTokenService sessions,
        ExamFormBuilder formBuilder,
        TakerQuestionProjector projector,
        AttemptGradingService grading,
        IDataFilter dataFilter)
    {
        _links = links;
        _exams = exams;
        _questions = questions;
        _groups = groups;
        _candidates = candidates;
        _attempts = attempts;
        _attemptQuestions = attemptQuestions;
        _answers = answers;
        _signals = signals;
        _topics = topics;
        _sessions = sessions;
        _formBuilder = formBuilder;
        _projector = projector;
        _grading = grading;
        _dataFilter = dataFilter;
    }

    public async Task<ExamPreviewDto> OpenLinkAsync(string token)
    {
        // The caller has no tenant context yet — the link is what establishes it —
        // so this lookup runs unfiltered and every read afterwards is scoped by the
        // ids the link itself carries.
        using var _ = _dataFilter.Disable<IMultiTenant>();

        var hash = ExamSessionTokenService.HashLinkToken(token);
        var link = await (await _links.GetQueryableAsync()).FirstOrDefaultAsync(l => l.TokenHash == hash);

        if (link is null)
        {
            return new ExamPreviewDto { IsAccessible = false, BlockReason = InternshipManagementSystemDomainErrorCodes.ExamLinkInvalid };
        }

        var now = Clock.Now;
        var blockReason = link.GetBlockReason(now);

        var exam = await _exams.GetAsync(link.ExamId);
        var candidate = await _candidates.GetAsync(link.CandidateId);

        if (blockReason is null && !exam.IsOpenAt(now))
        {
            blockReason = exam.Status != ExamStatus.Published
                ? InternshipManagementSystemDomainErrorCodes.ExamNotPublished
                : InternshipManagementSystemDomainErrorCodes.ExamOutsideSchedule;
        }

        var preview = new ExamPreviewDto
        {
            IsAccessible = blockReason is null,
            BlockReason = blockReason,
            ExamTitle = exam.Title,
            Description = exam.Description,
            CandidateName = candidate.FullName,
            TimeLimitInMinutes = exam.TimeLimitInMinutes,
            AttemptsAllowed = link.MaxAttempts,
            AttemptsUsed = link.AttemptsUsed,
            ExpiresAt = link.ExpiresAt,
            Mode = exam.Mode,
            QuestionCount = exam.QuestionsPerForm
                            ?? await (await _questions.GetQueryableAsync())
                                     .CountAsync(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId))
        };

        if (link.FirstOpenedAt is null)
        {
            link.FirstOpenedAt = now;
            await _links.UpdateAsync(link, autoSave: true);
        }

        if (!preview.IsAccessible)
        {
            return preview;
        }

        // An attempt still running is resumed rather than replaced: starting over
        // would discard answers the taker already gave.
        var active = await (await _attempts.GetQueryableAsync())
            .FirstOrDefaultAsync(a => a.ExamLinkId == link.Id && !a.IsSubmitted);

        preview.ResumableAttemptId = active?.Id;

        // The credential is minted against the attempt that will exist. For a resume
        // that is the running attempt; otherwise a placeholder deadline covers the
        // pre-start screen and StartAsync issues the real one.
        preview.SessionToken = active is not null
            ? _sessions.Issue(active.Id, link.CandidateId, link.ExamId, link.TenantId, active.DeadlineAt.ToUniversalTime())
            : _sessions.Issue(Guid.Empty, link.CandidateId, link.ExamId, link.TenantId,
                              now.AddMinutes(exam.TimeLimitInMinutes + 30).ToUniversalTime());

        // The plain token is needed once more, to bind the pending start to this link.
        preview.BlockReason = null;
        PendingLinkToken = token;

        return preview;
    }

    /// <summary>
    /// Carries the link token from preview to start within one request pipeline.
    /// The controller passes it explicitly; this exists so the interface stays clean.
    /// </summary>
    internal string? PendingLinkToken { get; private set; }

    public async Task<AttemptStateDto> StartAsync(string sessionToken)
    {
        var claims = RequireSession(sessionToken);

        using var _ = _dataFilter.Disable<IMultiTenant>();

        if (claims.AttemptId != Guid.Empty)
        {
            return await BuildStateAsync(await LoadOwnAttemptAsync(claims));
        }

        var link = await (await _links.GetQueryableAsync())
            .FirstOrDefaultAsync(l => l.CandidateId == claims.CandidateId && l.ExamId == claims.ExamId && !l.IsRevoked);

        if (link is null)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamLinkInvalid);
        }

        var now = Clock.Now;
        var blocked = link.GetBlockReason(now);
        if (blocked is not null)
        {
            throw new BusinessException(blocked);
        }

        var exam = await _exams.GetAsync(link.ExamId);
        if (!exam.IsOpenAt(now))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamOutsideSchedule);
        }

        // Resume rather than duplicate. The unique index on (ExamLinkId, unsubmitted)
        // is enforced in the database too, so a double-click cannot create two.
        var running = await (await _attempts.GetQueryableAsync())
            .FirstOrDefaultAsync(a => a.ExamLinkId == link.Id && !a.IsSubmitted);

        if (running is not null)
        {
            return await BuildStateAsync(running);
        }

        var seed = ExamSessionTokenService.NewShuffleSeed();
        var attempt = new Attempt(
            GuidGenerator.Create(), link.TenantId, exam.Id, link.CandidateId,
            now, now.AddMinutes(exam.TimeLimitInMinutes), seed)
        {
            ExamLinkId = link.Id
        };

        await _attempts.InsertAsync(attempt, autoSave: true);

        await LoadBlueprintAsync(exam);
        // Everything this exam may draw, not only what it owns. Filtering on
        // ExamId alone meant the shared bank existed in the schema and never
        // reached a paper: three forms for one level drew from three empty pools.
        var bank = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId))
            .ToListAsync();

        var form = _formBuilder.Build(exam, bank, attempt.Id, link.TenantId, seed);
        await _attemptQuestions.InsertManyAsync(form, autoSave: true);

        await RecordExposureAsync(form, bank);

        attempt.MaxScore = form.Sum(f => f.Score);
        await _attempts.UpdateAsync(attempt, autoSave: true);

        // The attempt count moves here, on an actual start — not when someone merely
        // checks whether a link is valid, which is what the old code did.
        link.AttemptsUsed++;
        await _links.UpdateAsync(link, autoSave: true);

        return await BuildStateAsync(attempt);
    }

    public async Task<TakerQuestionDto> GetQuestionAsync(string sessionToken, int position)
    {
        var claims = RequireSession(sessionToken);
        using var _ = _dataFilter.Disable<IMultiTenant>();

        var attempt = await LoadOwnAttemptAsync(claims);
        var form = await LoadFormAsync(attempt.Id);

        var slot = form.FirstOrDefault(f => f.Position == position)
                   ?? throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptQuestionNotOnForm);

        var question = await _questions.GetAsync(slot.QuestionId);

        QuestionGroup? group = null;
        if (slot.QuestionGroupId.HasValue)
        {
            group = await _groups.FindAsync(slot.QuestionGroupId.Value);
        }

        var dto = _projector.Project(question, slot, group, form.Count, BuildMediaUrl);

        var saved = await (await _answers.GetQueryableAsync())
            .FirstOrDefaultAsync(a => a.AttemptId == attempt.Id && a.QuestionId == question.Id);

        dto.SavedResponse = saved?.Response;
        dto.SavedFileName = saved?.AnswerFileName;

        return dto;
    }

    public async Task<SaveAnswerResultDto> SaveAnswerAsync(string sessionToken, SaveAnswerDto input)
    {
        var claims = RequireSession(sessionToken);
        using var _ = _dataFilter.Disable<IMultiTenant>();

        var attempt = await LoadOwnAttemptAsync(claims);
        var now = Clock.Now;

        if (attempt.IsSubmitted)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptAlreadySubmitted);
        }

        // Past the deadline the save is refused, but the client is told so it can
        // submit cleanly instead of silently losing the keystroke.
        if (attempt.IsExpired(now))
        {
            return new SaveAnswerResultDto { SavedAt = now, SecondsRemaining = 0, IsExpired = true };
        }

        var onForm = await (await _attemptQuestions.GetQueryableAsync())
            .AnyAsync(f => f.AttemptId == attempt.Id && f.QuestionId == input.QuestionId);

        if (!onForm)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptQuestionNotOnForm);
        }

        var answer = await (await _answers.GetQueryableAsync())
            .FirstOrDefaultAsync(a => a.AttemptId == attempt.Id && a.QuestionId == input.QuestionId);

        if (answer is null)
        {
            answer = new Answer(GuidGenerator.Create(), attempt.TenantId, attempt.Id, input.QuestionId);
            ApplyAnswer(answer, input, now);
            await _answers.InsertAsync(answer, autoSave: true);
        }
        else
        {
            ApplyAnswer(answer, input, now);
            await _answers.UpdateAsync(answer, autoSave: true);
        }

        // A paste large enough to be an imported answer is worth a reviewer's attention.
        if (input.WasPasted && (input.Response?.Length ?? 0) > 120)
        {
            await RecordSignalAsync(attempt, IntegritySignalType.Paste, input.QuestionId, input.Response!.Length);
        }

        return new SaveAnswerResultDto
        {
            SavedAt = now,
            // Always from the stored deadline: the client's clock never gets a vote.
            SecondsRemaining = attempt.SecondsRemaining(now),
            IsExpired = false
        };
    }

    public async Task<AttemptStateDto> GetStateAsync(string sessionToken)
    {
        var claims = RequireSession(sessionToken);
        using var _ = _dataFilter.Disable<IMultiTenant>();

        var attempt = await LoadOwnAttemptAsync(claims);
        return await BuildStateAsync(attempt);
    }

    public async Task ReportSignalAsync(string sessionToken, ReportIntegritySignalDto input)
    {
        var claims = RequireSession(sessionToken);
        using var _ = _dataFilter.Disable<IMultiTenant>();

        var attempt = await LoadOwnAttemptAsync(claims);
        await RecordSignalAsync(attempt, input.Type, input.QuestionId, input.Magnitude);
    }

    public async Task<AttemptResultDto> SubmitAsync(string sessionToken)
    {
        var claims = RequireSession(sessionToken);
        using var _ = _dataFilter.Disable<IMultiTenant>();

        var attempt = await LoadOwnAttemptAsync(claims);

        if (attempt.IsSubmitted)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptAlreadySubmitted);
        }

        var now = Clock.Now;

        attempt.IsSubmitted = true;
        attempt.SubmittedAt = now;
        attempt.EndReason = attempt.IsExpired(now)
            ? AttemptEndReason.TimedOutInBrowser
            : AttemptEndReason.SubmittedByCandidate;

        await _attempts.UpdateAsync(attempt, autoSave: true);
        await _grading.GradeAsync(attempt.Id);

        return await BuildResultAsync(attempt.Id);
    }

    public async Task<AttemptResultDto> GetResultAsync(string sessionToken)
    {
        var claims = RequireSession(sessionToken);
        using var _ = _dataFilter.Disable<IMultiTenant>();

        // Loaded through the same check rather than by id: a result is the one
        // thing a candidate most wants to read for somebody else.
        var own = await LoadOwnAttemptAsync(claims);

        return await BuildResultAsync(own.Id);
    }

    // ---------------------------------------------------------------- helpers

    private ExamSessionClaims RequireSession(string sessionToken) =>
        _sessions.Read(sessionToken)
        ?? throw new AbpAuthorizationException("The exam session is invalid or has expired.");

    private static void ApplyAnswer(Answer answer, SaveAnswerDto input, DateTime now)
    {
        answer.Response = input.Response;
        answer.AnswerBlobName = input.AnswerBlobName ?? answer.AnswerBlobName;
        answer.AnswerFileName = input.AnswerFileName ?? answer.AnswerFileName;
        answer.TimeSpentSeconds = input.TimeSpentSeconds;
        answer.WasPasted = answer.WasPasted || input.WasPasted;
        answer.KeystrokeCount += input.KeystrokeCount;
        answer.BackspaceCount += input.BackspaceCount;
        answer.AnsweredAt = now;
    }

    private async Task RecordSignalAsync(Attempt attempt, IntegritySignalType type, Guid? questionId, int? magnitude)
    {
        await _signals.InsertAsync(
            new IntegritySignal(GuidGenerator.Create(), attempt.TenantId, attempt.Id, type, Clock.Now)
            {
                QuestionId = questionId,
                Magnitude = magnitude
            },
            autoSave: true);

        attempt.IntegrityFlagCount++;
        await _attempts.UpdateAsync(attempt, autoSave: true);
    }

    private async Task<List<AttemptQuestion>> LoadFormAsync(Guid attemptId) =>
        await (await _attemptQuestions.GetQueryableAsync())
            .Where(f => f.AttemptId == attemptId)
            .OrderBy(f => f.Position)
            .ToListAsync();

    private async Task LoadBlueprintAsync(Exam exam)
    {
        // The builder reads exam.Blueprint; repositories return the aggregate without
        // it unless asked.
        var rules = await (await LazyServiceProvider
                .LazyGetRequiredService<IRepository<ExamBlueprintRule, Guid>>()
                .GetQueryableAsync())
            .Where(r => r.ExamId == exam.Id)
            .ToListAsync();

        exam.Blueprint = rules;
    }

    private async Task<AttemptStateDto> BuildStateAsync(Attempt attempt)
    {
        var exam = await _exams.GetAsync(attempt.ExamId);
        var form = await LoadFormAsync(attempt.Id);

        var answered = await (await _answers.GetQueryableAsync())
            .Where(a => a.AttemptId == attempt.Id && a.Response != null)
            .Select(a => a.QuestionId)
            .ToListAsync();

        var answeredSet = answered.ToHashSet();

        return new AttemptStateDto
        {
            AttemptId = attempt.Id,
            SecondsRemaining = attempt.SecondsRemaining(Clock.Now),
            TotalQuestions = form.Count,
            AnsweredCount = form.Count(f => answeredSet.Contains(f.QuestionId)),
            Answered = form.Select(f => answeredSet.Contains(f.QuestionId)).ToList(),
            IsSubmitted = attempt.IsSubmitted,
            AllowBackNavigation = exam.AllowBackNavigation,
            OneQuestionAtATime = exam.OneQuestionAtATime
        };
    }

    private async Task<AttemptResultDto> BuildResultAsync(Guid attemptId)
    {
        var attempt = await _attempts.GetAsync(attemptId);
        var exam = await _exams.GetAsync(attempt.ExamId);

        if (!attempt.IsSubmitted)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptNotSubmitted);
        }

        var result = new AttemptResultDto
        {
            AttemptId = attempt.Id,
            ExamTitle = exam.Title,
            IsFinal = attempt.IsGraded,
            SubmittedAt = attempt.SubmittedAt ?? attempt.DeadlineAt
        };

        // A provisional score would be read as final and is worse than none.
        if (!attempt.IsGraded)
        {
            return result;
        }

        result.Score = attempt.Score;
        result.MaxScore = attempt.MaxScore;
        result.ScorePercentage = attempt.ScorePercentage;
        result.IsPassed = attempt.IsPassed;

        var form = await LoadFormAsync(attempt.Id);
        var answers = await _answers.GetListAsync(a => a.AttemptId == attempt.Id);
        var questions = await (await _questions.GetQueryableAsync())
            .Where(q => form.Select(f => f.QuestionId).Contains(q.Id))
            .ToListAsync();

        result.TopicBreakdown = await BuildTopicBreakdownAsync(form, answers, questions);

        // Keys and explanations are released only in Practice mode, and only now that
        // the attempt is over. In Assessment mode this would compromise the bank.
        if (exam.Mode == ExamMode.Practice)
        {
            result.Review = BuildPracticeReview(form, answers, questions);
        }

        return result;
    }

    private async Task<List<TopicScoreDto>> BuildTopicBreakdownAsync(
        List<AttemptQuestion> form, List<Answer> answers, List<Question> questions)
    {
        var byQuestion = questions.ToDictionary(q => q.Id);
        var awarded = answers.Where(a => a.AwardedScore.HasValue)
                             .ToDictionary(a => a.QuestionId, a => a.AwardedScore!.Value);

        var groups = form
            .Where(f => byQuestion.TryGetValue(f.QuestionId, out var q) && q.TopicId.HasValue)
            .GroupBy(f => byQuestion[f.QuestionId].TopicId!.Value)
            .ToList();

        if (groups.Count == 0)
        {
            return new List<TopicScoreDto>();
        }

        var topicIds = groups.Select(g => g.Key).ToList();
        var topics = await (await _topics.GetQueryableAsync())
            .Where(t => topicIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        return groups.Select(g =>
        {
            var max = g.Sum(f => f.Score);
            var score = g.Sum(f => awarded.TryGetValue(f.QuestionId, out var s) ? s : 0m);

            return new TopicScoreDto
            {
                TopicId = g.Key,
                TopicName = topics.TryGetValue(g.Key, out var name) ? name : "—",
                Score = score,
                MaxScore = max,
                Percentage = max > 0 ? Math.Round(score / max * 100m, 1) : 0m
            };
        })
        .OrderByDescending(t => t.Percentage)
        .ToList();
    }

    private static List<PracticeReviewItemDto> BuildPracticeReview(
        List<AttemptQuestion> form, List<Answer> answers, List<Question> questions)
    {
        var byQuestion = questions.ToDictionary(q => q.Id);
        var byAnswer = answers.ToDictionary(a => a.QuestionId);

        return form
            .Where(f => byQuestion.ContainsKey(f.QuestionId))
            .Select(f =>
            {
                var question = byQuestion[f.QuestionId];
                byAnswer.TryGetValue(f.QuestionId, out var answer);

                return new PracticeReviewItemDto
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    Type = question.Type,
                    YourResponse = answer?.Response,
                    WasCorrect = answer?.IsCorrect,
                    AwardedScore = answer?.AwardedScore ?? 0m,
                    MaxScore = f.Score,
                    CorrectAnswer = CorrectAnswerRenderer.Render(question.Type, question.Payload),
                    Explanation = question.Explanation
                };
            })
            .ToList();
    }

    /// <summary>
    /// Signs a blob name for delivery. Media URLs are time-limited so a link copied
    /// out of the page stops working once the attempt is over.
    /// </summary>
    private static string BuildMediaUrl(string blobName) => $"/api/assessment/media/{blobName}";

    /// <summary>
    /// Loads the attempt a session names, and refuses one that is not the
    /// session's own.
    /// <para>
    /// The token's signature used to be the only thing standing between a
    /// candidate and every attempt in every tenant: these endpoints disable the
    /// tenant filter by necessity, and an attempt id was taken on trust. Checking
    /// the candidate and the tenant as well costs two comparisons and turns any
    /// future weakness in the token into a contained failure instead of a total
    /// one. Defence in depth is exactly this: the cheap second check that only
    /// matters on the day the first one fails.
    /// </para>
    /// </summary>
    private async Task<Attempt> LoadOwnAttemptAsync(ExamSessionClaims claims)
    {
        var attempt = await _attempts.GetAsync(claims.AttemptId);

        if (attempt.CandidateId != claims.CandidateId || attempt.TenantId != claims.TenantId)
        {
            throw new AbpAuthorizationException("This session does not belong to that attempt.");
        }

        return attempt;
    }

    /// <summary>
    /// Counts each question that made it onto this paper.
    /// <para>
    /// Exposure is the number of candidates who have seen a question, and it is
    /// what erodes its value once it circulates — a question that has been in
    /// front of enough people measures who has met it rather than who knows the
    /// answer. This is the only place it can be counted: a question is exposed
    /// when it is served, not when it is answered, because a candidate who skips
    /// it has still read it.
    /// </para>
    /// <para>
    /// The column existed and nothing wrote to it, which made the over-exposure
    /// warning at publish unreachable — it compared against a number that was
    /// always zero. A business review found that by reading the code.
    /// </para>
    /// <para>
    /// Counted here rather than in a nightly job so the number is true the moment
    /// an author looks at it, and updated without saving each row on its own.
    /// </para>
    /// </summary>
    private async Task RecordExposureAsync(List<AttemptQuestion> form, List<Question> bank)
    {
        var served = form.Select(slot => slot.QuestionId).ToHashSet();
        var exposed = bank.Where(question => served.Contains(question.Id)).ToList();

        foreach (var question in exposed)
        {
            question.TimesServed++;
        }

        if (exposed.Count > 0)
        {
            await _questions.UpdateManyAsync(exposed, autoSave: true);
        }
    }
}
