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

        // Questions whose statistics moved while grading this attempt. Collected
        // and saved once rather than a row at a time.
        var touched = new List<Question>();

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
                // A grader that throws goes to a human, exactly as a missing one
                // does. It has to: the response is a string a candidate chose, and
                // an unhandled exception here rolls back the whole submission. The
                // attempt then cannot be submitted at all, and when the deadline
                // worker force-submits it, it commits IsSubmitted before grading
                // and swallows the failure — leaving an attempt submitted,
                // ungraded, scored zero, and in nobody's review queue. A candidate
                // who knows they have failed could reach that state on purpose.
                GradeResult result;

                try
                {
                    result = grader.Grade(question.Payload, answer.Response, slot.Score);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Grader for {Type} failed on answer {AnswerId}; routed to manual review.",
                        question.Type,
                        answer.Id);

                    result = GradeResult.Manual("The automatic grader could not score this answer.");
                }

                answer.NeedsManualReview = result.NeedsManualReview;
                answer.IsCorrect = result.IsCorrect;
                answer.AwardedScore = result.NeedsManualReview ? null : result.AwardedScore;

                RecordOutcome(question, result.IsCorrect);
                touched.Add(question);
            }

            await _answers.UpdateAsync(answer, autoSave: false);
        }

        if (touched.Count > 0)
        {
            await _questions.UpdateManyAsync(touched, autoSave: false);
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

    /// <summary>
    /// Folds one graded answer into a question's running difficulty.
    /// <para>
    /// The difficulty index is the share of takers who got a question right, and
    /// it is what tells an author whether a question is hard or broken: near zero
    /// usually means the key is wrong rather than the question is difficult, and
    /// near one means it separates nobody.
    /// </para>
    /// <para>
    /// A running mean rather than a recount over every answer ever given, so
    /// grading one attempt costs one update instead of a scan of the whole
    /// history. The arithmetic is exact: the previous mean times the previous
    /// count, plus this outcome, over the new count.
    /// </para>
    /// <para>
    /// Answers waiting on a human are skipped. Counting them as wrong would make
    /// every essay question look impossible until somebody marked it.
    /// </para>
    /// </summary>
    private static void RecordOutcome(Question question, bool? isCorrect)
    {
        if (isCorrect is not { } correct)
        {
            return;
        }

        var previousCount = question.TimesAnswered;
        var previousMean = question.DifficultyIndex ?? 0m;

        question.TimesAnswered = previousCount + 1;

        question.DifficultyIndex = Math.Round(
            ((previousMean * previousCount) + (correct ? 1m : 0m)) / question.TimesAnswered,
            4,
            MidpointRounding.AwayFromZero);
    }
}
