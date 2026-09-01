using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.Results.Dtos;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Results;

/// <summary>
/// What happened when people sat the exam.
/// <para>
/// Written after a business review found that results reached nobody. Every
/// permission for this existed and nothing implemented them: an all-multiple-
/// choice paper was marked in milliseconds and then appeared on no screen in the
/// product, because the only results view was the manual-marking queue and that
/// filters to sittings a person still has to mark. Forty students sat the exam
/// and the coordinator had to ask them what their phones said.
/// </para>
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Results.View)]
/// <remarks>
/// No generated controller. GetListAsync and GetAsync both became
/// <c>GET api/app/result</c> — GetAsync takes an attempt id that is not named
/// <c>id</c>, so it stayed a query parameter and the two paths collided — and
/// Swashbuckle refused to build the whole document, so /swagger answered 500 and
/// the API browser had been gone for some time with nobody filing anything.
/// <para>
/// Nothing is lost by switching it off: <c>ResultController</c> exposes every
/// one of these at <c>/api/assessment/results</c>, with the route, the verb and
/// the permission written where a person can read them. The generated pair was
/// a second, undocumented way in that no client called.
/// </para>
/// </remarks>
[RemoteService(IsEnabled = false)]
public class ResultAppService : ApplicationService, IResultAppService
{
    private readonly IRepository<Attempt, Guid> _attempts;
    private readonly IRepository<AttemptQuestion, Guid> _attemptQuestions;
    private readonly IRepository<Answer, Guid> _answers;
    private readonly IRepository<Candidate, Guid> _candidates;
    private readonly IRepository<CandidateGroupMember, Guid> _members;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<ExamForm, Guid> _forms;
    private readonly IRepository<ExamLink, Guid> _links;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<Topic, Guid> _topics;
    private readonly IRepository<ExamSection, Guid> _sections;

    public ResultAppService(
        IRepository<Attempt, Guid> attempts,
        IRepository<AttemptQuestion, Guid> attemptQuestions,
        IRepository<Answer, Guid> answers,
        IRepository<Candidate, Guid> candidates,
        IRepository<CandidateGroupMember, Guid> members,
        IRepository<Exam, Guid> exams,
        IRepository<ExamForm, Guid> forms,
        IRepository<ExamLink, Guid> links,
        IRepository<Question, Guid> questions,
        IRepository<Topic, Guid> topics,
        IRepository<ExamSection, Guid> sections)
    {
        _attempts = attempts;
        _attemptQuestions = attemptQuestions;
        _answers = answers;
        _candidates = candidates;
        _members = members;
        _exams = exams;
        _forms = forms;
        _links = links;
        _questions = questions;
        _topics = topics;
        _sections = sections;
    }

    public async Task<PagedResultDto<ResultRowDto>> GetListAsync(ResultListRequestDto input)
    {
        var query = await FilterAsync(input);

        var totalCount = await query.CountAsync();

        var attempts = await query
            // Most recent first. A coordinator opening this screen is nearly always
            // looking at the sitting that just finished.
            .OrderByDescending(a => a.StartedAt)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<ResultRowDto>(totalCount, await ToRowsAsync(attempts));
    }

    public async Task<ResultSummaryDto> GetSummaryAsync(ResultListRequestDto input)
    {
        var query = await FilterAsync(input);

        // Materialised because the percentiles below cannot be expressed in SQL
        // that every provider agrees on, and a cohort is hundreds of rows rather
        // than millions.
        var attempts = await query
            .Select(a => new
            {
                a.IsSubmitted,
                a.IsGraded,
                a.NeedsManualReview,
                a.IsPassed,
                a.ScorePercentage,
            })
            .ToListAsync();

        var summary = new ResultSummaryDto
        {
            Sat = attempts.Count,
            AwaitingMarking = attempts.Count(a => a.IsSubmitted && !a.IsGraded),
        };

        // Only settled sittings count towards pass and fail. A paper waiting on a
        // marker is not a fail, and counting it as one makes the headline figure
        // drift down all morning and back up in the afternoon.
        var settled = attempts.Where(a => a.IsGraded).ToList();

        summary.Passed = settled.Count(a => a.IsPassed);
        summary.Failed = settled.Count(a => !a.IsPassed);

        if (settled.Count > 0)
        {
            var scores = settled.Select(a => a.ScorePercentage).OrderBy(s => s).ToList();

            summary.AverageScorePercentage = Math.Round(scores.Average(), 1);
            summary.HighestScorePercentage = scores[^1];
            summary.LowestScorePercentage = scores[0];

            // The middle score, which says more than the mean when a few people
            // opened the paper and walked away.
            summary.MedianScorePercentage = scores.Count % 2 == 1
                ? scores[scores.Count / 2]
                : Math.Round((scores[scores.Count / 2 - 1] + scores[scores.Count / 2]) / 2m, 1);
        }

        summary.NotStarted = await CountNotStartedAsync(input);

        return summary;
    }

    public async Task<ResultDetailDto> GetAsync(Guid attemptId)
    {
        var attempt = await _attempts.GetAsync(attemptId);
        var row = (await ToRowsAsync([attempt])).Single();

        var slots = await (await _attemptQuestions.GetQueryableAsync())
            .Where(q => q.AttemptId == attemptId)
            .OrderBy(q => q.Position)
            .ToListAsync();

        var answers = await (await _answers.GetQueryableAsync())
            .Where(a => a.AttemptId == attemptId)
            .ToListAsync();

        var questionIds = slots.Select(s => s.QuestionId).ToList();

        var questions = await (await _questions.GetQueryableAsync())
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var topicNames = await TopicNamesAsync(questions.Select(q => q.TopicId));

        var byQuestion = questions.ToDictionary(q => q.Id);
        var byAnswer = answers.ToDictionary(a => a.QuestionId);

        var rows = new List<ResultAnswerDto>();

        foreach (var slot in slots)
        {
            // A question deleted after the sitting leaves the row rather than
            // removing it: the marks were earned on a paper that had it, and a
            // total that does not add up is worse than a line saying so.
            var question = byQuestion.GetValueOrDefault(slot.QuestionId);
            var answer = byAnswer.GetValueOrDefault(slot.QuestionId);

            rows.Add(new ResultAnswerDto
            {
                QuestionId = slot.QuestionId,
                Position = slot.Position,
                QuestionText = question?.Text ?? string.Empty,
                Type = question?.Type ?? string.Empty,
                TopicName = question?.TopicId is { } topicId ? topicNames.GetValueOrDefault(topicId) : null,
                Response = answer?.Response,
                AnswerFileName = answer?.AnswerFileName,
                IsCorrect = answer?.IsCorrect,
                AwardedScore = answer?.AwardedScore ?? 0m,
                MaxScore = slot.Score,
                NeedsManualReview = answer?.NeedsManualReview ?? false,
                ReviewComment = answer?.ReviewComment,
                TimeSpentSeconds = answer?.TimeSpentSeconds,
            });
        }

        return new ResultDetailDto
        {
            Summary = row,
            Answers = rows,
            ByTopic = Breakdown(rows),

            // The same marks read the other way. A topic is what a question
            // measures; a section is where it sat on the paper — and a placement
            // coordinator reads the paper's own parts back, because that is what
            // the candidate sat and what the report to the student names.
            BySection = await BySectionAsync(slots, byAnswer),
        };
    }

    [Authorize(InternshipManagementSystemPermissions.Results.Export)]
    public async Task<string> ExportCsvAsync(ResultListRequestDto input)
    {
        var query = await FilterAsync(input);

        var attempts = await query.OrderByDescending(a => a.StartedAt).ToListAsync();
        var rows = await ToRowsAsync(attempts);

        var csv = new StringBuilder();

        // A BOM, because the overwhelmingly likely next step is opening this in
        // Excel — and without one it renders every Arabic name as mojibake, which
        // is the sort of thing that makes people stop trusting the export.
        csv.Append('﻿');

        csv.AppendLine(string.Join(',', new[]
        {
            "Candidate", "Email", "Exam", "Form", "Started", "Submitted",
            "Score", "MaxScore", "Percentage", "Result", "Minutes", "IntegrityFlags",
        }));

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Escape(row.CandidateName),
                Escape(row.CandidateEmail),
                Escape(row.ExamTitle),
                Escape(row.FormName ?? string.Empty),
                Escape(row.StartedAt.ToString("u", CultureInfo.InvariantCulture)),
                Escape(row.SubmittedAt?.ToString("u", CultureInfo.InvariantCulture) ?? string.Empty),
                row.Score.ToString(CultureInfo.InvariantCulture),
                row.MaxScore.ToString(CultureInfo.InvariantCulture),
                row.ScorePercentage.ToString(CultureInfo.InvariantCulture),

                // Three states, not two. A sitting still waiting on a marker is
                // neither a pass nor a fail, and writing "fail" there would be a
                // lie somebody acts on.
                Escape(!row.IsGraded ? "Pending" : row.IsPassed ? "Pass" : "Fail"),
                row.DurationInMinutes.ToString(CultureInfo.InvariantCulture),
                row.IntegrityFlagCount.ToString(CultureInfo.InvariantCulture),
            }));
        }

        return csv.ToString();
    }

    [Authorize(InternshipManagementSystemPermissions.Results.ViewItemAnalysis)]
    public async Task<List<ItemAnalysisRowDto>> GetItemAnalysisAsync(Guid examId)
    {
        var attemptIds = await (await _attempts.GetQueryableAsync())
            .Where(a => a.ExamId == examId && a.IsGraded)
            .Select(a => new { a.Id, a.ScorePercentage })
            .ToListAsync();

        if (attemptIds.Count == 0)
        {
            return [];
        }

        var ids = attemptIds.Select(a => a.Id).ToList();

        var answers = await (await _answers.GetQueryableAsync())
            .Where(a => ids.Contains(a.AttemptId) && a.AwardedScore != null)
            .Select(a => new { a.AttemptId, a.QuestionId, a.AwardedScore })
            .ToListAsync();

        var slots = await (await _attemptQuestions.GetQueryableAsync())
            .Where(q => ids.Contains(q.AttemptId))
            .Select(q => new { q.AttemptId, q.QuestionId, q.Score })
            .ToListAsync();

        var maxByQuestion = slots
            .GroupBy(s => s.QuestionId)
            .ToDictionary(g => g.Key, g => g.Max(s => s.Score));

        // The top and bottom quarter by total score. Discrimination is the
        // difference between how those two groups did on this one question: if the
        // strongest quarter does worse than the weakest, the key is usually wrong.
        var ranked = attemptIds.OrderByDescending(a => a.ScorePercentage).ToList();
        var groupSize = Math.Max(1, ranked.Count / 4);

        var top = ranked.Take(groupSize).Select(a => a.Id).ToHashSet();
        var bottom = ranked.TakeLast(groupSize).Select(a => a.Id).ToHashSet();

        // Whether the two groups are actually different. With a cohort clustered
        // on one total the split is decided by whatever order the database
        // returned, and the resulting number is an artifact of row order rather
        // than a property of the question. Below a real gap, discrimination is
        // reported as unknown instead of as a finding.
        var spread = ranked.Count > 1
            ? ranked.Take(groupSize).Average(a => a.ScorePercentage)
              - ranked.TakeLast(groupSize).Average(a => a.ScorePercentage)
            : 0m;

        var groupsAreComparable = spread >= 5m;

        var questionIds = maxByQuestion.Keys.ToList();

        var questions = await (await _questions.GetQueryableAsync())
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync();

        var topicNames = await TopicNamesAsync(questions.Select(q => q.TopicId));

        var rows = new List<ItemAnalysisRowDto>();

        foreach (var question in questions)
        {
            var forThis = answers.Where(a => a.QuestionId == question.Id).ToList();

            if (forThis.Count == 0)
            {
                continue;
            }

            var max = maxByQuestion.GetValueOrDefault(question.Id);

            if (max <= 0)
            {
                continue;
            }

            // Proportion of the marks available, rather than right-or-wrong, so
            // partially credited types are not counted as failures.
            decimal? Share(IEnumerable<decimal> awarded)
            {
                var values = awarded.ToList();

                // Null, not zero. A group that never answered this question has
                // told us nothing, and scoring that as "everybody got it wrong" is
                // what produced the worst false report this screen could make.
                return values.Count == 0 ? null : values.Average() / max;
            }

            var facility = Share(forThis.Select(a => a.AwardedScore!.Value)) ?? 0m;

            var topShare = Share(forThis.Where(a => top.Contains(a.AttemptId)).Select(a => a.AwardedScore!.Value));
            var bottomShare = Share(forThis.Where(a => bottom.Contains(a.AttemptId)).Select(a => a.AwardedScore!.Value));

            // Only when both groups actually answered it, and only when the two
            // groups differ enough to be two groups.
            //
            // Papers differ within one exam: named forms are assigned per class, so
            // the top quarter can be entirely people who sat Form A. Every Form B
            // question then had no top-quarter answers at all, scored zero, and was
            // reported as "the strongest candidates got this wrong" — a whole form
            // of correctly keyed questions flagged as mis-keyed, sorted to the top
            // of the list.
            decimal? discrimination = groupsAreComparable && topShare is { } t && bottomShare is { } b
                ? t - b
                : null;

            rows.Add(new ItemAnalysisRowDto
            {
                QuestionId = question.Id,
                Text = question.Text,
                Type = question.Type,
                TopicName = question.TopicId is { } topicId ? topicNames.GetValueOrDefault(topicId) : null,
                TimesAnswered = forThis.Count,
                Facility = Math.Round(facility, 2),
                Discrimination = discrimination is { } d ? Math.Round(d, 2) : null,
                FlagKey = Flag(forThis.Count, facility, discrimination),
            });
        }

        // Worst first: the point of the screen is the questions to fix, and putting
        // them at the bottom of an alphabetical list is how they stay unfixed. A
        // question whose discrimination could not be measured sorts after the ones
        // that could, because "unknown" is not a finding.
        return rows
            .OrderBy(r => r.Discrimination.HasValue ? 0 : 1)
            .ThenBy(r => r.Discrimination)
            .ToList();
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// What the numbers say, when they say anything.
    /// <para>
    /// Only where there is enough data to mean it. Flagging a question three
    /// people have answered teaches an author to ignore the flags.
    /// </para>
    /// </summary>
    private static string? Flag(int answered, decimal facility, decimal? discrimination)
    {
        if (answered < 20)
        {
            return null;
        }

        // Facility first, because it is measured on everybody who answered and
        // needs no comparison between groups. The two facility flags are true
        // whatever the cohort looked like.
        if (facility >= 0.95m)
        {
            return "IMS:ItemAnalysis:TooEasy";
        }

        if (facility <= 0.15m)
        {
            return "IMS:ItemAnalysis:TooHard";
        }

        if (discrimination is not { } value)
        {
            // Not measurable here: either group answered none of this question, or
            // the cohort's totals were too close together for the split to mean
            // anything. Saying nothing is right — a flag nobody can trust teaches
            // an author to ignore all of them.
            return null;
        }

        if (value < 0m)
        {
            return "IMS:ItemAnalysis:NegativeDiscrimination";
        }

        if (value < 0.1m)
        {
            return "IMS:ItemAnalysis:WeakDiscrimination";
        }

        return null;
    }

    /// <summary>
    /// One sitting's marks, grouped by the part of the exam each question was
    /// served under.
    /// <para>
    /// Read off the frozen paper, not off the questions. An author who re-files a
    /// question or deletes a section next term must not silently rewrite what a
    /// result from last term says; a section deleted since keeps its marks and
    /// loses only its name.
    /// </para>
    /// <para>
    /// In the exam's authored order rather than sorted, because this is read back
    /// against the paper. Empty on an exam with no sections, and a section that
    /// contributed nothing to this paper never appears — an empty heading claims a
    /// part of the exam happened that did not.
    /// </para>
    /// </summary>
    private async Task<List<SectionScoreDto>> BySectionAsync(
        List<AttemptQuestion> slots,
        Dictionary<Guid, Answer> byAnswer)
    {
        var served = slots.Where(s => s.ExamSectionId.HasValue).ToList();

        if (served.Count == 0)
        {
            return [];
        }

        var ids = served.Select(s => s.ExamSectionId!.Value).Distinct().ToList();

        var sections = await (await _sections.GetQueryableAsync())
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => new { s.Name, s.DisplayOrder });

        return served
            .GroupBy(s => s.ExamSectionId!.Value)
            .Select(group =>
            {
                var max = group.Sum(s => s.Score);
                var score = group.Sum(s => byAnswer.GetValueOrDefault(s.QuestionId)?.AwardedScore ?? 0m);
                var section = sections.GetValueOrDefault(group.Key);

                return new
                {
                    Order = section?.DisplayOrder ?? int.MaxValue,
                    Dto = new SectionScoreDto
                    {
                        SectionId = group.Key,
                        SectionName = section?.Name ?? string.Empty,
                        QuestionCount = group.Count(),
                        Score = score,
                        MaxScore = max,
                        ScorePercentage = max > 0 ? Math.Round(score / max * 100m, 1) : 0m,
                    },
                };
            })
            .OrderBy(row => row.Order)
            .Select(row => row.Dto)
            .ToList();
    }

    private static List<TopicScoreDto> Breakdown(List<ResultAnswerDto> answers)
    {
        return answers
            .GroupBy(a => a.TopicName)
            .Select(group =>
            {
                var max = group.Sum(a => a.MaxScore);
                var score = group.Sum(a => a.AwardedScore);

                return new TopicScoreDto
                {
                    TopicName = group.Key ?? string.Empty,
                    QuestionCount = group.Count(),
                    Score = score,
                    MaxScore = max,
                    ScorePercentage = max > 0 ? Math.Round(score / max * 100m, 1) : 0m,
                };
            })
            .OrderBy(t => t.TopicName)
            .ToList();
    }

    private async Task<Dictionary<Guid, string>> TopicNamesAsync(IEnumerable<Guid?> topicIds)
    {
        var ids = topicIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await (await _topics.GetQueryableAsync())
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);
    }

    private async Task<IQueryable<Attempt>> FilterAsync(ResultListRequestDto input)
    {
        var query = await _attempts.GetQueryableAsync();

        if (input.ExamId is { } examId)
        {
            query = query.Where(a => a.ExamId == examId);
        }

        if (input.ExamFormId is { } formId)
        {
            query = query.Where(a => a.ExamFormId == formId);
        }

        if (input.CandidateGroupId is { } groupId)
        {
            var memberIds = (await _members.GetQueryableAsync())
                .Where(m => m.CandidateGroupId == groupId)
                .Select(m => m.CandidateId);

            query = query.Where(a => memberIds.Contains(a.CandidateId));
        }

        if (input.PassedOnly == true)
        {
            query = query.Where(a => a.IsGraded && a.IsPassed);
        }

        if (input.AwaitingMarking == true)
        {
            query = query.Where(a => a.IsSubmitted && !a.IsGraded);
        }

        if (input.From is { } from)
        {
            query = query.Where(a => a.StartedAt >= from);
        }

        if (input.To is { } to)
        {
            query = query.Where(a => a.StartedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim();

            // The one definition, so this box and the roll's box answer the
            // same question. These three searched the raw name only, which meant
            // a person found on one screen could not be found on the next.
            var candidateIds = (await _candidates.GetQueryableAsync())
                .Where(CandidateSearch.Matching(term))
                .Select(c => c.Id);

            query = query.Where(a => candidateIds.Contains(a.CandidateId));
        }

        return query;
    }

    /// <summary>
    /// People sent a link for this exam who never started.
    /// <para>
    /// The number a coordinator chases, and one that no attempt row can carry
    /// because the whole point is that there is no attempt.
    /// </para>
    /// </summary>
    private async Task<int> CountNotStartedAsync(ResultListRequestDto input)
    {
        if (input.ExamId is not { } examId)
        {
            return 0;
        }

        var links = (await _links.GetQueryableAsync())
            .Where(l => l.ExamId == examId && !l.IsRevoked);

        // Narrowed by the same filters as everything else on the card. It used to
        // count every un-started link in the exam regardless, so a coordinator
        // looking at one class read "12 sat, 40 never started" — two numbers from
        // one card describing two different populations.
        if (input.CandidateGroupId is { } groupId)
        {
            var memberIds = (await _members.GetQueryableAsync())
                .Where(m => m.CandidateGroupId == groupId)
                .Select(m => m.CandidateId);

            links = links.Where(l => memberIds.Contains(l.CandidateId));
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim();

            // The one definition, so this box and the roll's box answer the
            // same question. These three searched the raw name only, which meant
            // a person found on one screen could not be found on the next.
            var candidateIds = (await _candidates.GetQueryableAsync())
                .Where(CandidateSearch.Matching(term))
                .Select(c => c.Id);

            links = links.Where(l => candidateIds.Contains(l.CandidateId));
        }

        var attempts = await _attempts.GetQueryableAsync();

        return await links.Where(l => !attempts.Any(a => a.ExamLinkId == l.Id)).CountAsync();
    }

    private async Task<List<ResultRowDto>> ToRowsAsync(List<Attempt> attempts)
    {
        if (attempts.Count == 0)
        {
            return [];
        }

        var showIntegrity = await AuthorizationService.IsGrantedAsync(
            InternshipManagementSystemPermissions.Review.ViewIntegritySignals);

        var candidateIds = attempts.Select(a => a.CandidateId).Distinct().ToList();
        var examIds = attempts.Select(a => a.ExamId).Distinct().ToList();
        var formIds = attempts.Where(a => a.ExamFormId != null)
            .Select(a => a.ExamFormId!.Value).Distinct().ToList();

        var candidates = await (await _candidates.GetQueryableAsync())
            .Where(c => candidateIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new { c.FullName, c.Email });

        var exams = await (await _exams.GetQueryableAsync())
            .Where(e => examIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Title);

        var forms = formIds.Count == 0
            ? []
            : await (await _forms.GetQueryableAsync())
                .Where(f => formIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.Name);

        return attempts.Select(attempt =>
        {
            var candidate = candidates.GetValueOrDefault(attempt.CandidateId);

            // The clock stops at submission, or at the deadline when nobody
            // submitted. Measuring an abandoned sitting to "now" would show a
            // duration that grows while the screen is open.
            var ended = attempt.SubmittedAt ?? attempt.DeadlineAt;

            return new ResultRowDto
            {
                AttemptId = attempt.Id,
                CandidateId = attempt.CandidateId,
                CandidateName = candidate?.FullName ?? string.Empty,
                CandidateEmail = candidate?.Email ?? string.Empty,
                ExamId = attempt.ExamId,
                ExamTitle = exams.GetValueOrDefault(attempt.ExamId) ?? string.Empty,
                FormName = attempt.ExamFormId is { } id ? forms.GetValueOrDefault(id) : null,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                IsSubmitted = attempt.IsSubmitted,
                IsGraded = attempt.IsGraded,
                NeedsManualReview = attempt.NeedsManualReview,
                Score = attempt.Score,
                MaxScore = attempt.MaxScore,
                ScorePercentage = attempt.ScorePercentage,
                IsPassed = attempt.IsPassed,
                EndReason = attempt.EndReason.ToString(),
                EndedByReason = attempt.EndedByReason,

                // Withheld from anyone without the permission that guards them
                // elsewhere. A count of "this candidate pasted four times" is an
                // accusation, and it was leaking through the roster and the CSV
                // to everyone who could read a score.
                IntegrityFlagCount = showIntegrity ? attempt.IntegrityFlagCount : 0,
                DurationInMinutes = Math.Max(0, (int)(ended - attempt.StartedAt).TotalMinutes),
            };
        }).ToList();
    }

    /// <summary>
    /// One CSV field.
    /// <para>
    /// Quoted whenever it contains a comma, a quote or a newline — a candidate
    /// called "Smith, John" otherwise shifts every column after it by one, and
    /// nobody notices until the marks are attached to the wrong people.
    /// </para>
    /// </summary>
    private static string Escape(string value)
    {
        // A cell beginning =, +, - or @ is evaluated by Excel and by Sheets. A
        // candidate registering as =HYPERLINK("http://x","click") gets that run on
        // the coordinator's machine when they open the export — and the stated
        // purpose of this file is that somebody opens it in a spreadsheet.
        //
        // A leading apostrophe is the conventional defusing: the text is shown and
        // nothing is evaluated.
        var safe = value.Length > 0 && "=+-@".Contains(value[0])
            ? "'" + value
            : value;

        if (!safe.Contains(',') && !safe.Contains('"') && !safe.Contains('\n') && !safe.Contains('\r'))
        {
            return safe;
        }

        return '"' + safe.Replace("\"", "\"\"") + '"';
    }
}
