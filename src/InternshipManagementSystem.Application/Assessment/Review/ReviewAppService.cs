using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.Review.Dtos;
using InternshipManagementSystem.Localization;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Review;

/// <summary>
/// The manual-grading queue and the marking screen behind it.
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Review.Default)]
public class ReviewAppService : ApplicationService, IReviewAppService
{
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<Answer, Guid> _answers;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestions;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<IntegritySignal, Guid> _signals;
    private readonly AttemptGradingService _grading;

    public ReviewAppService(
        IRepository<Attempt, Guid> attempts,
        IRepository<Answer, Guid> answers,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Question, Guid> questions,
        IRepository<Exam, Guid> exams,
        IRepository<Candidate, Guid> candidates,
        IRepository<IntegritySignal, Guid> signals,
        AttemptGradingService grading)
    {
        _attempts = attempts;
        _answers = answers;
        _attemptQuestions = attemptQuestions;
        _questions = questions;
        _exams = exams;
        _candidates = candidates;
        _signals = signals;
        _grading = grading;

        // This class derives from ApplicationService rather than from
        // InternshipManagementSystemAppService, so `L` has to be pointed at the
        // product's own resource explicitly or the integrity sentences below
        // resolve against ABP's DefaultResource and come back as bare keys.
        LocalizationResource = typeof(InternshipManagementSystemResource);
    }

    /// <summary>Oldest first: a candidate waiting longest is served first.</summary>
    [Authorize(InternshipManagementSystemPermissions.Review.ViewQueue)]
    public async Task<PagedResultDto<ReviewQueueItemDto>> GetQueueAsync(ReviewQueueRequestDto input)
    {
        var attempts = await _attempts.GetQueryableAsync();
        var candidates = await _candidates.GetQueryableAsync();
        var exams = await _exams.GetQueryableAsync();

        // Waiting: oldest first, because somebody has been waiting longest for it.
        // Already marked: newest first, because a mark being revisited is nearly
        // always one just made.
        var query = input.Finished
            ? from attempt in attempts
              join candidate in candidates on attempt.CandidateId equals candidate.Id
              join exam in exams on attempt.ExamId equals exam.Id
              where attempt.IsSubmitted && !attempt.NeedsManualReview
              orderby attempt.SubmittedAt descending
              select new { attempt, candidate.FullName, exam.Title }
            : from attempt in attempts
              join candidate in candidates on attempt.CandidateId equals candidate.Id
              join exam in exams on attempt.ExamId equals exam.Id
              where attempt.IsSubmitted && attempt.NeedsManualReview
              orderby attempt.SubmittedAt
              select new { attempt, candidate.FullName, exam.Title };

        var totalCount = await query.CountAsync();
        var page = await query.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync();

        var attemptIds = page.Select(p => p.attempt.Id).ToList();

        var pendingCounts = await (await _answers.GetQueryableAsync())
            .Where(a => attemptIds.Contains(a.AttemptId) && a.NeedsManualReview)
            .GroupBy(a => a.AttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AttemptId, x => x.Count);

        // The same rule the results roster applies, applied here too.
        //
        // A count of "this candidate pasted four times" is an accusation, and
        // the roster withholds it from anyone without the permission that guards
        // it. This queue emitted it to anyone who could open the queue at all —
        // so the number a marker was not trusted with on one screen arrived
        // unasked on another. A rule enforced in one place is not a rule.
        var showIntegrity = await AuthorizationService.IsGrantedAsync(
            InternshipManagementSystemPermissions.Review.ViewIntegritySignals);

        var items = page.Select(p => new ReviewQueueItemDto
        {
            AttemptId = p.attempt.Id,
            CandidateName = p.FullName,
            ExamTitle = p.Title,
            SubmittedAt = p.attempt.SubmittedAt ?? p.attempt.DeadlineAt,
            PendingCount = pendingCounts.TryGetValue(p.attempt.Id, out var c) ? c : 0,
            ProvisionalScore = p.attempt.Score,
            MaxScore = p.attempt.MaxScore,
            IntegrityFlagCount = showIntegrity ? p.attempt.IntegrityFlagCount : 0
        }).ToList();

        return new PagedResultDto<ReviewQueueItemDto>(totalCount, items);
    }

    /// <summary>
    /// The answers on one attempt that need a human, with the rubric, the key and
    /// the behavioural context the reviewer needs to judge them.
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Review.ViewQueue)]
    public async Task<List<ReviewAnswerDto>> GetAnswersAsync(Guid attemptId)
    {
        var pending = await (await _answers.GetQueryableAsync())
            .Where(a => a.AttemptId == attemptId && a.NeedsManualReview)
            .ToListAsync();

        if (pending.Count == 0)
        {
            return [];
        }

        var questionIds = pending.Select(a => a.QuestionId).ToList();

        var questions = await (await _questions.GetQueryableAsync())
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id);

        var slots = await (await _attemptQuestions.GetQueryableAsync())
            .Where(f => f.AttemptId == attemptId && questionIds.Contains(f.QuestionId))
            .ToDictionaryAsync(f => f.QuestionId, f => f.Score);

        return pending
            .Where(a => questions.ContainsKey(a.QuestionId))
            .Select(a =>
            {
                var question = questions[a.QuestionId];
                var rubric = PayloadJson.Read<RubricPayload>(question.Payload);

                return new ReviewAnswerDto
                {
                    AnswerId = a.Id,
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    MaxScore = slots.TryGetValue(question.Id, out var score) ? score : question.Score,
                    Response = a.Response,
                    AnswerFileName = a.AnswerFileName,
                    AnswerFileUrl = a.AnswerBlobName is null ? null : $"/api/assessment/media/{a.AnswerBlobName}",
                    Rubric = rubric?.Criteria.Select(c => new RubricCriterionDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        MaxScore = c.MaxScore
                    }).ToList() ?? [],
                    ReviewerGuidance = rubric?.ReviewerGuidance,
                    CorrectAnswer = CorrectAnswerRenderer.Render(question.Type, question.Payload),
                    Explanation = question.Explanation,
                    AwardedScore = a.AwardedScore,
                    ReviewComment = a.ReviewComment,
                    ReviewedAt = a.ReviewedAt,
                    WasPasted = a.WasPasted,
                    TimeSpentSeconds = a.TimeSpentSeconds,
                    KeystrokeCount = a.KeystrokeCount,
                    BackspaceCount = a.BackspaceCount
                };
            })
            .ToList();
    }

    /// <summary>
    /// Records a mark and immediately retotals the attempt.
    /// <para>
    /// The retotal is the whole point. The previous implementation saved the score on
    /// the answer and stopped, so the attempt kept its pre-review total and stayed in
    /// this queue permanently. Marking without recalculating is not marking.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Review.Grade)]
    public async Task GradeAnswerAsync(GradeAnswerDto input)
    {
        var answer = await _answers.GetAsync(input.AnswerId);

        var slot = await (await _attemptQuestions.GetQueryableAsync())
            .FirstOrDefaultAsync(f => f.AttemptId == answer.AttemptId && f.QuestionId == answer.QuestionId);

        var maxScore = slot?.Score ?? 0m;

        if (input.AwardedScore > maxScore)
        {
            throw new BusinessException("IMS:Review:ScoreAboveMaximum")
                .WithData("Awarded", input.AwardedScore)
                .WithData("Maximum", maxScore);
        }

        answer.AwardedScore = input.AwardedScore;
        answer.IsCorrect = maxScore > 0 && input.AwardedScore >= maxScore;
        answer.ReviewComment = input.Comment;
        answer.ReviewedBy = CurrentUser.Id;
        answer.ReviewedAt = Clock.Now;
        answer.NeedsManualReview = false;

        if (input.RubricScores is { Count: > 0 })
        {
            answer.RubricScores = PayloadJson.Write(input.RubricScores);
        }

        await _answers.UpdateAsync(answer, autoSave: true);

        // Retotal now, and close the attempt out if this was the last pending answer.
        await _grading.RecalculateAsync(answer.AttemptId);
    }

    /// <summary>
    /// What was observed during an attempt.
    /// <para>
    /// Behavioural facts with a plain-language summary, never a score or a verdict.
    /// Text-based AI detectors are unreliable and misfire hardest on people writing
    /// in a second language, so accusing someone on their prose is indefensible. How
    /// the text reached the page is observable; what it means is a person's call.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Review.ViewIntegritySignals)]
    public async Task<IntegrityReportDto> GetIntegrityReportAsync(Guid attemptId)
    {
        var signals = await (await _signals.GetQueryableAsync())
            .Where(s => s.AttemptId == attemptId)
            .OrderBy(s => s.OccurredAt)
            .ToListAsync();

        var report = new IntegrityReportDto
        {
            AttemptId = attemptId,
            Signals = signals.Select(s => new IntegritySignalDto
            {
                Type = s.Type,
                QuestionId = s.QuestionId,
                OccurredAt = s.OccurredAt,
                Magnitude = s.Magnitude
            }).ToList()
        };

        foreach (var group in signals.GroupBy(s => s.Type))
        {
            var count = group.Count();

            var magnitude = group.Sum(s => s.Magnitude ?? 0);

            // Read, not computed. These were seven interpolated English sentences
            // in a product whose default language is Arabic, sitting underneath a
            // heading and a lede that were both translated — so a marker working
            // in Arabic got Arabic chrome and an English list. The resource is the
            // only place a sentence a person reads is allowed to live.
            report.Observations.Add(group.Key switch
            {
                IntegritySignalType.Paste => L["Review:Observation:Paste", count, magnitude],
                IntegritySignalType.WindowBlur => L["Review:Observation:WindowBlur", count, magnitude],
                IntegritySignalType.ImplausibleSpeed => L["Review:Observation:ImplausibleSpeed", count],
                IntegritySignalType.NoCorrections => L["Review:Observation:NoCorrections", count],
                IntegritySignalType.DevToolsOpened => L["Review:Observation:DevToolsOpened", count],
                IntegritySignalType.PageReloaded => L["Review:Observation:PageReloaded", count],
                _ => L["Review:Observation:Other", count, group.Key]
            });
        }

        return report;
    }
}
