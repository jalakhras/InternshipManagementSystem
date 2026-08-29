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
using Volo.Abp.Uow;

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
    private readonly IUnitOfWorkManager _unitOfWork;
    private readonly ILogger<AttemptGradingService> _logger;

    public AttemptGradingService(
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<Question, Guid> questions,
        IRepository<Exam, Guid> exams,
        IGraderResolver graders,
        IUnitOfWorkManager unitOfWork,
        ILogger<AttemptGradingService> logger)
    {
        _attempts = attempts;
        _attemptQuestions = attemptQuestions;
        _answers = answers;
        _questions = questions;
        _exams = exams;
        _graders = graders;
        _unitOfWork = unitOfWork;
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

        // Which questions this attempt answered right and which it answered wrong.
        //
        // Ids, not entities. Every candidate sitting one exam answers the same
        // questions, so mutating those rows and saving them back put a whole
        // cohort in a race for the same concurrency stamps — a load test found
        // that of forty candidates submitting together, thirty-nine were refused.
        // The statistics are applied below as two set-based updates instead.
        var answeredCorrectly = new List<Guid>();
        var answeredWrongly = new List<Guid>();

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

                if (result.IsCorrect is { } correct)
                {
                    (correct ? answeredCorrectly : answeredWrongly).Add(question.Id);
                }
            }

            await _answers.UpdateAsync(answer, autoSave: false);
        }

        await RecordOutcomesAsync(answeredCorrectly, hit: true);
        await RecordOutcomesAsync(answeredWrongly, hit: false);

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
    /// <para>
    /// Applied as one statement per outcome rather than by loading each question
    /// and saving it back. The running mean is computed in the database from the
    /// row's own current values, so two candidates answering the same question at
    /// the same moment both count — where the read-modify-write version had them
    /// race for one concurrency stamp and refused all but the first. That refusal
    /// was not a lost statistic; it failed the whole submission.
    /// </para>
    /// </summary>
    private async Task RecordOutcomesAsync(List<Guid> questionIds, bool hit)
    {
        if (questionIds.Count == 0)
        {
            return;
        }

        // An int, not a decimal. As a decimal parameter this arrives typed wide
        // enough that SQL Server's own precision rules push the division's result
        // type past what the column can hold, and the whole submission fails with
        // an arithmetic overflow — which is how a working expression on paper
        // became a 500 for every candidate submitting at once.
        var scored = hit ? 1 : 0;

        // Sorted, so every request touches these rows in the same order. Forty
        // candidates submit at once and are served the same questions in different
        // shuffles; taking the locks in whatever order the paper happened to be in
        // is how they deadlocked with each other.
        var ordered = questionIds.OrderBy(id => id).ToList();

        // Outside the submission's transaction, and allowed to fail.
        //
        // These are statistics. A candidate's exam must not fail because a counter
        // could not be written — and inside the submission's own transaction it
        // would, because a deadlock aborts the whole thing and no amount of
        // catching downstream can save it. The submission is the thing that
        // matters; the difficulty index can miss one attempt.
        try
        {
            using var scope = _unitOfWork.Begin(requiresNew: true, isTransactional: false);

            // Both right-hand sides read the row as it stands before this
            // statement, which is what makes the mean correct: the new count is
            // used as the divisor explicitly rather than relying on the other
            // assignment.
            await (await _questions.GetQueryableAsync())
                .Where(question => ordered.Contains(question.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        q => q.DifficultyIndex,
                        q => Math.Round(
                            (((q.DifficultyIndex ?? 0m) * q.TimesAnswered) + scored) / (q.TimesAnswered + 1),
                            4))
                    .SetProperty(q => q.TimesAnswered, q => q.TimesAnswered + 1));

            await scope.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not record item statistics for {Count} question(s). The attempt is graded and "
                + "unaffected; the difficulty index for those questions is short by one answer.",
                ordered.Count);
        }
    }
}
