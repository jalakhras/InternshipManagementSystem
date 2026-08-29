using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Scores an attempt, and rescores it whenever a human changes something.
/// </summary>
public class AttemptGradingService : ITransientDependency
{
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestions;
    private readonly IRepository<Answer, Guid> _answers;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IGraderResolver _graders;
    private readonly ILogger<AttemptGradingService> _logger;

    public AttemptGradingService(
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<Question, Guid> questions,
        IRepository<Exam, Guid> exams,
        IGraderResolver graders,
        ILogger<AttemptGradingService> logger)
    {
        _attempts = attempts;
        _attemptQuestions = attemptQuestions;
        _answers = answers;
        _questions = questions;
        _exams = exams;
        _graders = graders;
        _logger = logger;
    }

    /// <summary>
    /// Runs every registered grader over the attempt, then totals it.
    /// <para>
    /// Questions are loaded in one query. The previous implementation fetched a
    /// question per answer inside the loop, so a 50-question exam issued 50 round
    /// trips at the exact moment a taker was waiting on their result.
    /// </para>
    /// </summary>
    public async Task GradeAsync(Guid attemptId)
    {
        var attempt = await _attempts.GetAsync(attemptId);
        var exam = await _exams.GetAsync(attempt.ExamId);

        var form = await LoadFormAsync(attemptId);
        var answers = await _answers.GetListAsync(a => a.AttemptId == attemptId);
        var questions = await LoadQuestionsAsync(form.Select(f => f.QuestionId).ToList());

        var answersByQuestion = answers.ToDictionary(a => a.QuestionId);

        foreach (var slot in form)
        {
            if (!questions.TryGetValue(slot.QuestionId, out var question))
            {
                continue;
            }

            // An unanswered question scores zero without needing a grader or a reviewer.
            if (!answersByQuestion.TryGetValue(slot.QuestionId, out var answer))
            {
                continue;
            }

            var grader = _graders.Resolve(question.Type);

            if (grader is null)
            {
                // A type nobody registered a grader for goes to a human. Scoring it
                // zero silently would penalise the taker for an authoring decision.
                answer.NeedsManualReview = true;
                answer.IsCorrect = null;
                answer.AwardedScore = null;
                _logger.LogWarning("No grader registered for question type {Type}; routed to manual review.", question.Type);
            }
            else
            {
                var result = grader.Grade(question.Payload, answer.Response, slot.Score);

                answer.NeedsManualReview = result.NeedsManualReview;
                answer.IsCorrect = result.IsCorrect;
                answer.AwardedScore = result.NeedsManualReview ? null : result.AwardedScore;
            }

            await _answers.UpdateAsync(answer, autoSave: false);
        }

        await RecalculateAsync(attempt, exam, form, answers);
    }

    /// <summary>
    /// Recomputes the total after a reviewer saves a mark, and closes the attempt
    /// out when nothing is left pending.
    /// <para>
    /// This is what the previous manual-review path was missing: it wrote
    /// <c>PartialScore</c> onto the answer and stopped. The attempt total was never
    /// touched and <c>NeedsManualReview</c> was never cleared, so every reviewed
    /// attempt kept its pre-review score and sat in the queue permanently.
    /// </para>
    /// </summary>
    public async Task RecalculateAsync(Guid attemptId)
    {
        var attempt = await _attempts.GetAsync(attemptId);
        var exam = await _exams.GetAsync(attempt.ExamId);
        var form = await LoadFormAsync(attemptId);
        var answers = await _answers.GetListAsync(a => a.AttemptId == attemptId);

        await RecalculateAsync(attempt, exam, form, answers);
    }

    private async Task RecalculateAsync(
        Attempt attempt,
        Exam exam,
        List<AttemptQuestion> form,
        List<Answer> answers)
    {
        var awardedByQuestion = answers
            .Where(a => a.AwardedScore.HasValue)
            .ToDictionary(a => a.QuestionId, a => a.AwardedScore!.Value);

        // The maximum comes from this taker's own form. Forms differ in length under
        // a blueprint, so a shared constant would be wrong for most attempts.
        var maxScore = form.Sum(f => f.Score);
        var score = form.Sum(f => awardedByQuestion.TryGetValue(f.QuestionId, out var s) ? s : 0m);

        var pending = answers.Count(a => a.NeedsManualReview);

        attempt.ApplyScore(score, maxScore, exam.PassingPercentage);
        attempt.NeedsManualReview = pending > 0;
        attempt.IsGraded = pending == 0;

        await _attempts.UpdateAsync(attempt, autoSave: true);

        _logger.LogInformation(
            "Attempt {AttemptId} scored {Score}/{MaxScore} ({Percentage}%), passed={Passed}, pending review={Pending}.",
            attempt.Id, attempt.Score, attempt.MaxScore, attempt.ScorePercentage, attempt.IsPassed, pending);
    }

    private async Task<List<AttemptQuestion>> LoadFormAsync(Guid attemptId)
    {
        var queryable = await _attemptQuestions.GetQueryableAsync();
        return await queryable.Where(q => q.AttemptId == attemptId)
                              .OrderBy(q => q.Position)
                              .ToListAsync();
    }

    private async Task<Dictionary<Guid, Question>> LoadQuestionsAsync(List<Guid> ids)
    {
        var queryable = await _questions.GetQueryableAsync();
        var list = await queryable.Where(q => ids.Contains(q.Id)).ToListAsync();
        return list.ToDictionary(q => q.Id);
    }
}
