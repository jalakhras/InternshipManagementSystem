using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Settings;
using InternshipManagementSystem.Assessment.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;

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
/// <remarks>
/// <b>Not audited.</b> ABP's audit log records a call's parameters, and every
/// method here is handed the candidate's session token — which this product
/// states plainly is their whole credential: they have no account, and the
/// token is the only thing that says who they are. Writing it into
/// <c>AbpAuditLogActions.Parameters</c> put a live credential in a table, in
/// plain text, for as long as the sitting lasts plus five minutes. Anybody able
/// to read that table could have taken the exam as them.
/// <para>
/// The answers went with it. <c>SaveAnswerAsync</c> was storing a second copy of
/// every response in the audit table, and the copy was outside everything the
/// product does to keep its promises about it: deleting an organisation clears
/// nineteen assessment tables and the files beside them, and does not touch the
/// audit log. "Everything recorded about you is removed" was true of the copy
/// the product knew about.
/// </para>
/// <para>
/// Nothing is lost by not auditing here. What a dispute actually rests on is the
/// product's own record — the answer rows, the attempt's timings, the integrity
/// signals — and all of it is written deliberately, kept deliberately, and
/// removed deliberately. The audit row was a duplicate that outlived its
/// original.
/// </para>
/// </remarks>
[DisableAuditing]
[AllowAnonymous]
public class ExamTakingAppService : ApplicationService, IExamTakingAppService
{
    private readonly IRepository<ExamLink, Guid> _links;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<QuestionGroup, Guid> _groups;
    private readonly IRepository<ExamSection, Guid> _sections;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestions;
    private readonly IRepository<Answer, Guid> _answers;
    private readonly IRepository<IntegritySignal, Guid> _signals;
    private readonly IRepository<Topic, Guid> _topics;
    private readonly IRepository<Assignment, Guid> _assignments;
    private readonly IRepository<ExamForm, Guid> _forms;
    private readonly IRepository<ExamFormQuestion, Guid> _formQuestions;
    private readonly ExamSessionTokenService _sessions;
    private readonly ExamFormBuilder _formBuilder;
    private readonly TakerQuestionProjector _projector;
    private readonly AttemptGradingService _grading;
    private readonly IDataFilter _dataFilter;

    /// <summary>Read directly, so a tenant lookup can refuse to fall back.</summary>
    private ISettingManager SettingManager =>
        LazyServiceProvider.LazyGetRequiredService<ISettingManager>();

    public ExamTakingAppService(
        IRepository<ExamLink, Guid> links,
        IRepository<Exam, Guid> exams,
        IRepository<Question, Guid> questions,
        IRepository<QuestionGroup, Guid> groups,
        IRepository<ExamSection, Guid> sections,
        IRepository<Candidate, Guid> candidates,
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<IntegritySignal, Guid> signals,
        IRepository<Topic, Guid> topics,
        IRepository<Assignment, Guid> assignments,
        IRepository<ExamForm, Guid> forms,
        IRepository<ExamFormQuestion, Guid> formQuestions,
        ExamSessionTokenService sessions,
        ExamFormBuilder formBuilder,
        TakerQuestionProjector projector,
        AttemptGradingService grading,
        IDataFilter dataFilter)
    {
        _links = links;
        _assignments = assignments;
        _forms = forms;
        _formQuestions = formQuestions;
        _exams = exams;
        _questions = questions;
        _groups = groups;
        _sections = sections;
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

        if (blockReason is null && !exam.IsOpenAt(await TenantNowAsync(link.TenantId, now)))
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
            QuestionCount = await ExpectedQuestionCountAsync(exam)
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
            ? _sessions.Issue(
                active.Id, link.CandidateId, link.ExamId, link.TenantId,
                active.DeadlineAt.ToUniversalTime(), link.Id)
            : _sessions.Issue(
                Guid.Empty, link.CandidateId, link.ExamId, link.TenantId,
                now.AddMinutes(exam.TimeLimitInMinutes + 30).ToUniversalTime(), link.Id);

        // Whose exam this is. Read after the link resolves, so it is the owning
        // tenant's branding rather than whoever happens to be signed in.
        //
        // Read inside the link's own tenant. Disabling the data filter lets this
        // request see the row; it does not make the request *be* that tenant, and
        // settings are resolved per tenant — so without the change here a language
        // centre's candidates were shown the platform's name and mark instead of
        // the centre's, which is the opposite of the point.
        using (CurrentTenant.Change(link.TenantId))
        {
            preview.OrganizationName = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.OrganizationName);

            preview.OrganizationBrandColor = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.BrandColor);

            // Where to write when something goes wrong. Read tenant-only, for
            // the reason the logo is: a candidate handed the host's address is
            // handed ours, and writing to us about a paper we do not run is a
            // message nobody can answer.
            preview.OrganizationSupportEmail = CurrentTenant.Id is null
                ? await SettingProvider.GetOrNullAsync(
                    InternshipManagementSystemSettings.SupportEmail)
                : await SettingManager.GetOrNullForTenantAsync(
                    InternshipManagementSystemSettings.SupportEmail,
                    CurrentTenant.Id.Value,
                    fallback: false);

            // This organisation's own logo only. Inheriting the host's gives a
            // candidate their academy's name beside a broken image, because the
            // file lives in the host's blob partition and their link cannot
            // reach it. No logo at all draws the astrolabe, which is a mark
            // rather than a hole.
            var logo = CurrentTenant.Id is null
                ? await SettingProvider.GetOrNullAsync(
                    InternshipManagementSystemSettings.LogoBlobName)
                : await SettingManager.GetOrNullForTenantAsync(
                    InternshipManagementSystemSettings.LogoBlobName,
                    CurrentTenant.Id.Value,
                    fallback: false);

            if (!string.IsNullOrWhiteSpace(logo))
            {
                // Signed like any other media a candidate is shown: they have no
                // account, so the address is the whole credential.
                preview.OrganizationLogoUrl = BuildMediaUrl(logo, link.ExpiresAt, link.TenantId);
            }
        }

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

        // The link this session was minted from, not "a link this candidate has to
        // this exam". A candidate can hold more than one — a resit is exactly that
        // — and resolving by candidate and exam took whichever row came back
        // first, so a student opening their second link could burn an attempt on
        // the first one.
        //
        // The fallback is for tokens minted before the claim existed. An exam in
        // progress must not end because we shipped.
        var links = await _links.GetQueryableAsync();

        var link = claims.LinkId is { } linkId && linkId != Guid.Empty
            ? await links.FirstOrDefaultAsync(l => l.Id == linkId && !l.IsRevoked)
            : await links.FirstOrDefaultAsync(l =>
                l.CandidateId == claims.CandidateId && l.ExamId == claims.ExamId && !l.IsRevoked);

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
        if (!exam.IsOpenAt(await TenantNowAsync(link.TenantId, now)))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamOutsideSchedule);
        }

        // Resume rather than duplicate. The unique index on (ExamLinkId, unsubmitted)
        // is enforced in the database too, so a double-click cannot create two.
        var running = await (await _attempts.GetQueryableAsync())
            .FirstOrDefaultAsync(a => a.ExamLinkId == link.Id && !a.IsSubmitted);

        if (running is not null)
        {
            return await StartedStateAsync(running, link);
        }

        // Which paper this sitting uses, decided when it was sent. Null means draw
        // one for this candidate, which is what every exam did before named forms
        // existed and still the right answer for practice.
        var assignment = await _assignments.FindAsync(link.AssignmentId);

        var formId = assignment?.ExamFormId
                     ?? await RotatedFormIdAsync(assignment, exam.Id, link.CandidateId);

        var seed = ExamSessionTokenService.NewShuffleSeed();
        var attempt = new Attempt(
            GuidGenerator.Create(), link.TenantId, exam.Id, link.CandidateId,
            now, now.AddMinutes(exam.TimeLimitInMinutes), seed)
        {
            ExamLinkId = link.Id,

            // Recorded rather than inferred later: a form can be retired after
            // somebody sat it, and a result only means what it meant if the paper
            // behind it is known.
            ExamFormId = formId,
        };

        try
        {
            await _attempts.InsertAsync(attempt, autoSave: true);
        }
        catch (DbUpdateException)
        {
            // Two starts arrived together.
            //
            // The check above looks for a running attempt and resumes it, which
            // handles a second tap that arrives after the first has finished.
            // It cannot handle two that arrive before either has written: both
            // pass the look, both reach the insert, and the unique index over
            // unsubmitted attempts per link keeps whichever gets there first.
            //
            // That index is doing its job and the data is right — there is one
            // sitting, and the attempt is spent once because the counter moves
            // below this line rather than above it. What was wrong is what the
            // loser was shown: a server error, on the screen where somebody
            // begins an exam, with no way to tell whether it started.
            //
            // Reported as something the screen can say instead. The candidate
            // presses again and the check above finds the sitting that won.
            throw new BusinessException(
                InternshipManagementSystemDomainErrorCodes.AttemptAlreadyStarting);
        }

        await LoadBlueprintAsync(exam);
        await LoadSectionsAsync(exam);
        // Everything this exam may draw, not only what it owns. Filtering on
        // ExamId alone meant the shared bank existed in the schema and never
        // reached a paper: three forms for one level drew from three empty pools.
        var bank = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId))
            .ToListAsync();

        var form = formId is { } id
            ? await BuildFromNamedFormAsync(id, exam, attempt.Id, link.TenantId, seed, bank)
            : _formBuilder.Build(exam, bank, attempt.Id, link.TenantId, seed);

        await _attemptQuestions.InsertManyAsync(form, autoSave: true);

        await RecordExposureAsync(form, bank);

        attempt.MaxScore = form.Sum(f => f.Score);
        await _attempts.UpdateAsync(attempt, autoSave: true);

        // The attempt count moves here, on an actual start — not when someone merely
        // checks whether a link is valid, which is what the old code did.
        link.AttemptsUsed++;
        await _links.UpdateAsync(link, autoSave: true);

        return await StartedStateAsync(attempt, link);
    }

    /// <summary>
    /// The state, plus the credential that names the attempt it describes.
    /// <para>
    /// The token minted on the preview screen carries the empty id, because the
    /// attempt did not exist yet. Only the start knows the real one, and every
    /// call after it reads the attempt out of the token — so the start has to hand
    /// back a replacement, and the caller has to use it.
    /// </para>
    /// <para>
    /// Its lifetime is the attempt's deadline rather than a fixed window: a
    /// credential that outlives the exam it opens is a way back into a submitted
    /// paper.
    /// </para>
    /// </summary>
    private async Task<AttemptStateDto> StartedStateAsync(Attempt attempt, ExamLink link)
    {
        var state = await BuildStateAsync(attempt);

        state.SessionToken = _sessions.Issue(
            attempt.Id,
            link.CandidateId,
            link.ExamId,
            link.TenantId,
            attempt.DeadlineAt.ToUniversalTime(),
            link.Id);

        return state;
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

        var dto = _projector.Project(
            question, slot, group, form.Count,
            blob => BuildMediaUrl(blob, attempt.DeadlineAt, attempt.TenantId),
            await PlacementAsync(form, slot));

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
        //
        // With one exception, and it is not a loophole: a file that was already
        // on its way. A recording is not a keystroke — somebody answering a
        // speaking question talks until told to stop, and a minute of audio then
        // has to travel. Refusing it at the instant the clock turns over does
        // not stop late work; it throws away work that was finished on time, and
        // that is exactly what happened: the file reached storage and the save
        // that would have attached it was refused, so the answer existed
        // everywhere except where anybody could see it.
        //
        // Only the attachment, never text. Someone still typing after time is up
        // is a different thing entirely, and this must not become a way to do it.
        var attachmentOnly =
            !string.IsNullOrWhiteSpace(input.AnswerBlobName) && string.IsNullOrWhiteSpace(input.Response);

        if (attempt.IsExpired(now) && !(attachmentOnly && attempt.IsWithinUploadGrace(now)))
        {
            return new SaveAnswerResultDto
            {
                SavedAt = now,
                SecondsRemaining = 0,
                IsExpired = true,

                // Nothing was written, and the screen must not say otherwise.
                Saved = false,
            };
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

        // Deliberately no paste signal here, and the reason matters.
        //
        // This was written when pasting was allowed, and it meant what it said:
        // an answer this long arriving at once is worth a reviewer's attention.
        // Then blocking landed. The text now never reaches the box — so there is
        // no imported answer to report, and the flag on the way in only says the
        // candidate *tried*.
        //
        // Left in place it did something worse than nothing. `WasPasted` stays
        // set for the rest of the question, and the paper autosaves every 800ms,
        // so one blocked Ctrl+V became a paste record on every save from then on,
        // each one carrying a magnitude equal to the candidate's own typing. The
        // marker opened a sitting and found a dozen quantified accusations of an
        // event that never happened — and the two honest signals were buried
        // underneath them.
        //
        // The attempt is recorded once, by the browser, at the moment it happens.
        // `WasPasted` is still stored on the answer: it is what stops a long
        // answer being called implausibly fast, which is the one thing it is
        // still evidence of.

        await NoteHowItWasWrittenAsync(attempt, input);

        return new SaveAnswerResultDto
        {
            SavedAt = now,
            // Always from the stored deadline: the client's clock never gets a vote.
            SecondsRemaining = attempt.SecondsRemaining(now),

            // Still true when a late attachment was accepted, and it has to be:
            // the file is kept, and the paper is still over. Saying otherwise
            // would hand the candidate back time they do not have.
            IsExpired = attempt.IsExpired(now),

            Saved = true,
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
        ?? throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamSessionExpired);

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

    /// <summary>
    /// Two things the browser already measured and nobody read.
    /// <para>
    /// <c>ImplausibleSpeed</c> and <c>NoCorrections</c> had names, translations,
    /// and a sentence each waiting on the marker's screen — and nothing produced
    /// either of them, so those sentences could never appear. The measurements
    /// they need arrive with every save already: how long the candidate was on
    /// the question, how many keys they pressed, how many were backspaces.
    /// </para>
    /// <para>
    /// Both are deliberately hard to trip. An observation a marker cannot trust
    /// is worse than none: it trains them to skim past all of them, including
    /// the one that mattered. So the speed bar is set at ten characters a second
    /// sustained — comfortably above a fast touch-typist, who runs at about
    /// seven — and the no-corrections bar needs a long answer typed with not one
    /// backspace, which is unusual in a way that composing carefully elsewhere
    /// and retyping is not.
    /// </para>
    /// <para>
    /// Neither fires on a pasted answer. Paste is already recorded and already
    /// explains both: text that arrives at once is infinitely fast and has no
    /// corrections. Reporting all three would be one event described three
    /// times, which reads as three findings.
    /// </para>
    /// </summary>
    private async Task NoteHowItWasWrittenAsync(Attempt attempt, SaveAnswerDto input)
    {
        var length = input.Response?.Length ?? 0;

        if (length < 200 || input.WasPasted)
        {
            return;
        }

        var seconds = input.TimeSpentSeconds ?? 0;

        if (seconds > 0 && length / seconds > 10)
        {
            await RecordSignalAsync(
                attempt, IntegritySignalType.ImplausibleSpeed, input.QuestionId, length / seconds);
        }

        // Typed, not arrived: enough keystrokes to account for the text. Without
        // this a long answer that appeared by some route the browser did not see
        // as a paste would be reported as flawless typing rather than as text
        // that was never typed.
        if (length >= 300 && input.BackspaceCount == 0 && input.KeystrokeCount >= length / 2)
        {
            await RecordSignalAsync(
                attempt, IntegritySignalType.NoCorrections, input.QuestionId, length);
        }
    }

    /// <summary>
    /// Records one observation, if this organisation and this exam collect them.
    /// <para>
    /// Both switches existed, both were written on their own screens, and
    /// neither was ever read — so an organisation that turned integrity
    /// recording off, and an author who left it off for a practice paper, were
    /// recorded anyway. The setting's own hint says what it will do. Watching
    /// people who were told they were not being watched is not a defaulting
    /// bug; it is the promise being false.
    /// </para>
    /// <para>
    /// Checked in the tenant the attempt belongs to, not whoever is signed in —
    /// nobody is: the candidate has no account, and the request runs with the
    /// multi-tenant filter disabled so it can see their row at all.
    /// </para>
    /// </summary>
    private async Task RecordSignalAsync(Attempt attempt, IntegritySignalType type, Guid? questionId, int? magnitude)
    {
        if (!await CollectsSignalsAsync(attempt))
        {
            return;
        }

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

    /// <summary>
    /// Whether anything should be observed at all: the organisation's setting
    /// first, then this exam's own switch. Either one off means off.
    /// </summary>
    private async Task<bool> CollectsSignalsAsync(Attempt attempt)
    {
        using (CurrentTenant.Change(attempt.TenantId))
        {
            var tenantWide = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.CollectIntegritySignals);

            if (bool.TryParse(tenantWide, out var enabled) && !enabled)
            {
                return false;
            }
        }

        var exam = await _exams.FindAsync(attempt.ExamId);

        return exam?.CollectIntegritySignals ?? true;
    }

    /// <summary>
    /// Now, as a clock on the wall in the organisation's own time zone.
    /// <para>
    /// A scheduled window is what a coordinator typed — nine in the morning where
    /// they are — so the moment it is compared against has to be the same kind of
    /// thing. The setting existed, its hint said "every exam clock and scheduled
    /// window is read in this zone; getting it wrong opens exams at the wrong
    /// hour", and nothing read it: the comparison used the server's own clock. On
    /// one machine in one country that is invisible. On a container in UTC
    /// serving a Riyadh academy it opens the exam three hours late, to a room
    /// full of people already sitting there.
    /// </para>
    /// <para>
    /// An unset or unrecognised zone falls back to the server's clock, which is
    /// the behaviour that was there before — a bad zone id must not stop an exam
    /// from opening at all.
    /// </para>
    /// </summary>
    private async Task<DateTime> TenantNowAsync(Guid? tenantId, DateTime serverNow)
    {
        string? id;

        using (CurrentTenant.Change(tenantId))
        {
            id = await SettingProvider.GetOrNullAsync(InternshipManagementSystemSettings.TimeZone);
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return serverNow;
        }

        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                serverNow.ToUniversalTime(), TimeZoneInfo.FindSystemTimeZoneById(id));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            Logger.LogWarning(
                "Tenant {TenantId} has an unusable time zone {TimeZone}; using the server clock.",
                tenantId, id);

            return serverNow;
        }
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

    /// <summary>
    /// How many questions the candidate is about to be asked, for the screen they
    /// read before the clock starts.
    /// <para>
    /// Section-aware, because it had to become so the moment a section's own
    /// count started being honoured. A placement test with twenty-four listening
    /// items and a draw of eight used to serve all twenty-four, so the bank's size
    /// was the right number to show; now it is not, and showing it would tell a
    /// candidate to budget for three times the paper they will get.
    /// </para>
    /// <para>
    /// An upper bound rather than a promise, in the same way the blueprint path
    /// has always been: a rule or a section that cannot fill itself contributes
    /// what it can rather than blocking somebody mid-exam.
    /// </para>
    /// </summary>
    private async Task<int> ExpectedQuestionCountAsync(Exam exam)
    {
        var pools = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId))
            .GroupBy(q => q.ExamSectionId)
            .Select(g => new { SectionId = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = pools.Sum(p => p.Count);

        var sections = await (await _sections.GetQueryableAsync())
            .Where(s => s.ExamId == exam.Id)
            .ToListAsync();

        if (sections.Count == 0)
        {
            // Exactly what it was before: the exam's own cap, or the whole bank.
            return exam.QuestionsPerForm is { } flat && flat < total ? flat : total;
        }

        var available = pools.Where(p => p.SectionId.HasValue)
                             .ToDictionary(p => p.SectionId!.Value, p => p.Count);

        // Questions the author has not filed under a section are served whole, so
        // they count in full.
        var unfiled = pools.FirstOrDefault(p => p.SectionId is null)?.Count ?? 0;

        return unfiled + sections.Sum(section =>
        {
            var pool = available.GetValueOrDefault(section.Id);

            return section.QuestionsPerForm is { } cap && cap < pool ? cap : pool;
        });
    }

    /// <summary>
    /// Loads the exam's parts, for the same reason the blueprint is loaded: the
    /// builder reads them off the aggregate and a repository does not bring them.
    /// <para>
    /// Forgetting this is silent. The paper still builds, and it builds flat — an
    /// exam laid out in four skills delivers as one undifferentiated list, which
    /// is exactly the state this work exists to end.
    /// </para>
    /// </summary>
    private async Task LoadSectionsAsync(Exam exam)
    {
        exam.Sections = await (await _sections.GetQueryableAsync())
            .Where(s => s.ExamId == exam.Id)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    /// <summary>
    /// Where one slot sits among the parts of this candidate's paper.
    /// <para>
    /// Counted over the frozen form, not over the authored exam: two candidates
    /// drawing different numbers of listening questions are each told the length
    /// of the section they are actually sitting.
    /// </para>
    /// </summary>
    private async Task<SectionPlacement?> PlacementAsync(List<AttemptQuestion> form, AttemptQuestion slot)
    {
        if (slot.ExamSectionId is not { } sectionId)
        {
            return null;
        }

        var section = await _sections.FindAsync(sectionId);

        if (section is null)
        {
            // Deleted since the sitting started. The paper keeps its marks and its
            // order; it simply stops naming a heading that no longer exists,
            // rather than failing the request a candidate is mid-exam in.
            return null;
        }

        var inSection = form.Where(f => f.ExamSectionId == sectionId).ToList();
        var position = inSection.FindIndex(f => f.Id == slot.Id) + 1;

        return new SectionPlacement(section, position, inSection.Count);
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
            OneQuestionAtATime = exam.OneQuestionAtATime,

            // Carried on every poll, because the moment it is needed is the
            // moment the first screen is long gone: somebody mid-paper whose
            // connection has just come back, or whose recording will not start.
            OrganizationSupportEmail = await SupportAddressAsync(attempt.TenantId)
        };
    }

    /// <summary>
    /// The organisation's own support address, or nothing.
    /// <para>
    /// Tenant-only, for the reason the logo is read that way. A candidate handed
    /// the host's address is handed ours, and a message to us about a paper we
    /// do not run is one nobody can answer. An organisation that published none
    /// has said so by leaving it empty.
    /// </para>
    /// </summary>
    private async Task<string?> SupportAddressAsync(Guid? tenantId)
    {
        using (CurrentTenant.Change(tenantId))
        {
            return tenantId is null
                ? await SettingProvider.GetOrNullAsync(
                    InternshipManagementSystemSettings.SupportEmail)
                : await SettingManager.GetOrNullForTenantAsync(
                    InternshipManagementSystemSettings.SupportEmail,
                    tenantId.Value,
                    fallback: false);
        }
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

        // The organisation may release results itself. The setting existed, its
        // hint said so in writing — "disable it where a person must approve the
        // result; a certificate that arrives before the coordinator sees it is
        // hard to withdraw" — and nothing read it, so every candidate saw their
        // score the moment marking finished.
        using (CurrentTenant.Change(attempt.TenantId))
        {
            var shown = await SettingProvider.GetOrNullAsync(
                InternshipManagementSystemSettings.ShowResultToCandidate);

            if (bool.TryParse(shown, out var visible) && !visible)
            {
                result.ScoreWithheld = true;
                return result;
            }
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

        // The same marks read the other way. A topic is what a question measures;
        // a section is where it sat on the paper, and a candidate who remembers
        // sitting "Listening" wants to see how they did on that. This is the half
        // of the placement-test story the competency profile could not tell.
        result.SectionBreakdown = await BuildSectionBreakdownAsync(form, answers);

        // What the marker wrote, in the order the questions were sat.
        //
        // The marking screen calls this box "Feedback for the candidate" and
        // says underneath that it is shown to them with their result. It was
        // stored and carried nowhere — the result had no field for it — so
        // every marker who took the trouble to write something wrote it to
        // nobody, and the screen kept telling them otherwise.
        //
        // Only reached when the score itself is being shown: the withheld and
        // not-yet-final branches above return before this line, and feedback
        // arriving ahead of the mark is the same problem the withholding setting
        // exists to prevent.
        result.Feedback = form
            .OrderBy(f => f.Position)
            .Select(f => answers.FirstOrDefault(a => a.QuestionId == f.QuestionId)?.ReviewComment)
            .Where(comment => !string.IsNullOrWhiteSpace(comment))
            .Select(comment => comment!)
            .ToList();

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

    /// <summary>
    /// The paper's marks, grouped by the part each question was served under.
    /// <para>
    /// Read off the frozen form rather than off the questions, which is the whole
    /// point of recording the section there: an author who re-files a question or
    /// deletes a section next term must not silently rewrite what an old result
    /// says. A section deleted since keeps its marks and loses only its name.
    /// </para>
    /// <para>
    /// Left in the exam's own order rather than sorted worst-first, unlike the
    /// topic breakdown: a candidate reads this against the paper they remember
    /// sitting, and reordering its parts makes that harder, not easier.
    /// </para>
    /// </summary>
    private async Task<List<SectionScoreDto>> BuildSectionBreakdownAsync(
        List<AttemptQuestion> form, List<Answer> answers)
    {
        var served = form.Where(f => f.ExamSectionId.HasValue).ToList();

        if (served.Count == 0)
        {
            return new List<SectionScoreDto>();
        }

        var awarded = answers.Where(a => a.AwardedScore.HasValue)
                             .ToDictionary(a => a.QuestionId, a => a.AwardedScore!.Value);

        var sectionIds = served.Select(f => f.ExamSectionId!.Value).Distinct().ToList();

        var sections = await (await _sections.GetQueryableAsync())
            .Where(s => sectionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => new { s.Name, s.DisplayOrder });

        return served
            .GroupBy(f => f.ExamSectionId!.Value)
            .Select(group =>
            {
                var max = group.Sum(f => f.Score);
                var score = group.Sum(f => awarded.TryGetValue(f.QuestionId, out var s) ? s : 0m);
                var section = sections.GetValueOrDefault(group.Key);

                return new
                {
                    Order = section?.DisplayOrder ?? int.MaxValue,
                    Dto = new SectionScoreDto
                    {
                        SectionId = group.Key,
                        SectionName = section?.Name ?? "—",
                        QuestionCount = group.Count(),
                        Score = score,
                        MaxScore = max,
                        Percentage = max > 0 ? Math.Round(score / max * 100m, 1) : 0m
                    }
                };
            })
            .OrderBy(row => row.Order)
            .Select(row => row.Dto)
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
    /// Signs a blob name for delivery, so a link copied out of the page stops
    /// working once the attempt is over.
    /// <para>
    /// The grant travels in the query string because it has nowhere else to go: an
    /// image or an audio clip is fetched by the browser itself, and nothing in the
    /// page can add a header to that request. It names this one blob and expires
    /// with this attempt.
    /// </para>
    /// </summary>
    private string BuildMediaUrl(string blobName, DateTime deadline, Guid? tenantId) =>
        $"/api/assessment/media/{blobName}?grant=" +
        Uri.EscapeDataString(
            _sessions.IssueMediaGrant(blobName, deadline.ToUniversalTime().AddMinutes(5), tenantId));

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
        // Found rather than fetched, because it may genuinely be gone: the
        // monitor lists running sittings and offers to throw one away, and the
        // person it happens to is answering a question at the time. Fetching
        // handed them the data layer's own sentence — a .NET type name and a
        // GUID — which tells somebody sitting an exam nothing they can act on,
        // not even whether it was their fault.
        var attempt = await _attempts.FindAsync(claims.AttemptId);

        if (attempt is null)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptNoLongerExists);
        }

        if (attempt.CandidateId != claims.CandidateId || attempt.TenantId != claims.TenantId)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamSessionMismatch);
        }

        await RefuseRevokedLinkAsync(attempt);

        return attempt;
    }

    /// <summary>
    /// Stops a sitting whose link has been revoked.
    /// <para>
    /// Revoking is described in this codebase as killing a link "that leaked or
    /// went to the wrong person", and it did nothing of the sort to a sitting
    /// already under way. The session token is self-contained and signed, so
    /// nothing on the way in asked whether the link behind it still stood: the
    /// person holding a leaked link carried on answering, and submitted.
    /// </para>
    /// <para>
    /// Which is the case revoking exists for. A link that leaked and is not being
    /// used needs no emergency; the emergency is somebody using it now, and the
    /// stop did not stop them.
    /// </para>
    /// <para>
    /// Checked here rather than at the door, because the door is opened once and
    /// a sitting lasts hours. One lookup by primary key on each call, on a path
    /// that already makes several — the cost of the check is not what makes it
    /// worth having; the hours between opening the paper and handing it in are.
    /// </para>
    /// <para>
    /// Nothing is discarded. The answers written so far stay exactly where they
    /// are, and what becomes of the attempt is a decision for the people who
    /// revoked the link — the monitor can end it or throw it away, and both of
    /// those are deliberate acts by somebody who knows why.
    /// </para>
    /// </summary>
    private async Task RefuseRevokedLinkAsync(Attempt attempt)
    {
        if (attempt.ExamLinkId is not { } linkId)
        {
            return;
        }

        var revoked = await (await _links.GetQueryableAsync())
            .Where(l => l.Id == linkId)
            .Select(l => l.IsRevoked)
            .FirstOrDefaultAsync();

        if (revoked)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamLinkRevoked);
        }
    }

    /// <summary>
    /// The next paper in the rotation, or null when this sitting does not rotate.
    /// <para>
    /// Chosen by how many times this candidate has already sat this exam, so the
    /// first sitting gets the first paper and a retake gets the next one. It
    /// wraps: with two papers and three attempts the third is the first paper
    /// again, which is honest — the alternative is refusing to let somebody sit
    /// an exam because the bank ran out of forms.
    /// </para>
    /// <para>
    /// Ordered by code rather than by creation, because the code is what a
    /// coordinator names when they talk about a paper, and creation order changes
    /// when somebody deletes a draft.
    /// </para>
    /// </summary>
    private async Task<Guid?> RotatedFormIdAsync(Assignment? assignment, Guid examId, Guid candidateId)
    {
        if (assignment?.RotateForms != true)
        {
            return null;
        }

        var published = await (await _forms.GetQueryableAsync())
            .Where(f => f.ExamId == examId && f.Status == ExamFormStatus.Published)
            .OrderBy(f => f.Code)
            .Select(f => f.Id)
            .ToListAsync();

        if (published.Count == 0)
        {
            // Nothing to rotate through. Drawing a paper is the older behaviour
            // and a working one; refusing to start would punish the candidate for
            // an authoring decision they know nothing about.
            return null;
        }

        var alreadySat = await (await _attempts.GetQueryableAsync())
            .CountAsync(a => a.ExamId == examId && a.CandidateId == candidateId);

        return published[alreadySat % published.Count];
    }

    /// <summary>
    /// Builds this attempt's paper from a named form.
    /// <para>
    /// The form's own order and its own marks, both frozen when it was published.
    /// Nothing is shuffled and nothing is drawn: two candidates who sat "Form 2"
    /// must have answered the same paper, which is the entire reason a named form
    /// exists.
    /// </para>
    /// <para>
    /// A question that has since been deleted is skipped rather than failing the
    /// start. A candidate sitting down must not be stopped by an authoring change
    /// made last week — the shortfall is visible in the marks and recoverable, and
    /// a locked-out candidate is not.
    /// </para>
    /// </summary>
    private async Task<List<AttemptQuestion>> BuildFromNamedFormAsync(
        Guid examFormId,
        Exam exam,
        Guid attemptId,
        Guid? tenantId,
        int seed,
        List<Question> bank)
    {
        var slots = await (await _formQuestions.GetQueryableAsync())
            .Where(q => q.ExamFormId == examFormId)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        var known = bank.ToDictionary(q => q.Id);

        // A named form exists to make two scores comparable, and a paper that has
        // silently lost a question is not comparable with the one everybody else
        // sat. The builder never fails — it contributes what it can — so a form
        // whose questions were deactivated after publication produced a shorter
        // paper marked out of a different total, with no signal to anyone.
        //
        // A missing question or two is recoverable and visible in the marks; an
        // empty paper is not, and a candidate must never reach one.
        var missing = slots.Count(slot => !known.ContainsKey(slot.QuestionId));

        if (missing > 0)
        {
            Logger.LogWarning(
                "Form {FormId} is serving {Served} of {Total} questions: {Missing} are no longer "
                + "drawable by exam {ExamId}. Scores from this sitting are not comparable with "
                + "earlier ones.",
                examFormId, slots.Count - missing, slots.Count, missing, exam.Id);
        }

        if (missing == slots.Count && slots.Count > 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormNoLongerUsable);
        }

        // Through the same projector the drawn path uses, which is the point: the
        // option order lives there. Written out by hand this method once omitted it,
        // and a matching question with no recorded order arrives with left[i] paired
        // to right[i] — the answer key, in the JSON handed to the candidate.
        var paper = slots
            .Where(slot => known.ContainsKey(slot.QuestionId))
            .Select(slot => new PaperSlot(known[slot.QuestionId], slot.Score))
            .ToList();

        var built = _formBuilder.Project(exam, paper, attemptId, tenantId, seed);

        // Exposure accrues per form as well as per question: a form in front of
        // enough people has circulated whatever its questions' individual counts
        // say, and that is the number a coordinator retires a paper on.
        //
        // Incremented in the database for the same reason as the question counts
        // above: a whole cohort sits one paper, and read-modify-write on a shared
        // row is a queue that most of them lose.
        await (await _forms.GetQueryableAsync())
            .Where(f => f.Id == examFormId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.TimesUsed, f => f.TimesUsed + 1));

        return built;
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
        var served = form.Select(slot => slot.QuestionId).ToList();

        if (served.Count == 0)
        {
            return;
        }

        // Sorted, so every request takes these locks in the same order. A cohort
        // starting together is served the same questions in different shuffles,
        // and locking them in paper order is how forty requests deadlocked.
        served = served.OrderBy(id => id).ToList();

        // One statement in the database, not a read-modify-write per row.
        //
        // Every candidate sitting the same exam is served the same questions, so
        // loading those rows and saving them back put forty people in a race for
        // the same handful of concurrency stamps. A load test found what that
        // costs: of forty candidates starting together, thirty-nine were refused
        // with a conflict and could not sit the exam at all. In a room, that is
        // thirty-nine people whose exam did not start.
        //
        // A counter is not something two writers can disagree about — "add one" is
        // the same instruction whoever runs it first — so it does not want the
        // optimistic check that entity tracking imposes.
        await (await _questions.GetQueryableAsync())
            .Where(question => served.Contains(question.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(q => q.TimesServed, q => q.TimesServed + 1));
    }
}
