using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>Authoring exams and their form blueprints.</summary>
[Authorize(InternshipManagementSystemPermissions.Exams.Default)]
public class ExamAppService : ApplicationService, IExamAppService
{
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<ExamBlueprintRule, Guid> _blueprint;
    private readonly IRepository<Category, Guid> _categories;
    private readonly IRepository<Level, Guid> _levels;
    private readonly IRepository<Topic, Guid> _topics;

    public ExamAppService(
        IRepository<Exam, Guid> exams,
        IRepository<Question, Guid> questions,
        IRepository<ExamBlueprintRule, Guid> blueprint,
        IRepository<Category, Guid> categories,
        IRepository<Level, Guid> levels,
        IRepository<Topic, Guid> topics)
    {
        _exams = exams;
        _questions = questions;
        _blueprint = blueprint;
        _categories = categories;
        _levels = levels;
        _topics = topics;
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<PagedResultDto<ExamDto>> GetListAsync(ExamListRequestDto input)
    {
        var exams = await _exams.GetQueryableAsync();
        var categories = await _categories.GetQueryableAsync();
        var levels = await _levels.GetQueryableAsync();
        var questions = await _questions.GetQueryableAsync();

        var query = exams.AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            // The description too. An author who wrote "for the January intake"
            // there and searches for it is not being unreasonable — the box says
            // search, not search titles, and it costs nothing to mean it.
            var term = input.Filter.Trim();

            query = query.Where(e =>
                e.Title.Contains(term) ||
                (e.Description != null && e.Description.Contains(term)));
        }

        if (input.CategoryId is { } categoryId)
        {
            query = query.Where(e => e.CategoryId == categoryId);
        }

        if (input.LevelId is { } levelId)
        {
            query = query.Where(e => e.LevelId == levelId);
        }

        if (input.Status is { } status)
        {
            query = query.Where(e => e.Status == status);
        }

        var totalCount = await query.CountAsync();

        // Counted in the database rather than by loading questions: an exam bank can
        // run to hundreds, and this list shows twenty exams at a time.
        var projected = from exam in query
                        join category in categories on exam.CategoryId equals category.Id into cat
                        from category in cat.DefaultIfEmpty()
                        join level in levels on exam.LevelId equals level.Id into lvl
                        from level in lvl.DefaultIfEmpty()
                        orderby exam.CreationTime descending
                        select new ExamDto
                        {
                            Id = exam.Id,
                            Title = exam.Title,
                            Description = exam.Description,
                            CategoryId = exam.CategoryId,
                            CategoryName = category != null ? category.Name : null,
                            LevelId = exam.LevelId,
                            LevelName = level != null ? level.Name : null,
                            Status = exam.Status,
                            Mode = exam.Mode,
                            TimeLimitInMinutes = exam.TimeLimitInMinutes,
                            PassingPercentage = exam.PassingPercentage,
                            QuestionsPerForm = exam.QuestionsPerForm,
                            // The same rule as the publish check and the delivery,
                            // written out because an Expression cannot be invoked
                            // inside a query the database has to translate. Kept
                            // beside Question.DrawableBy on purpose: if one of them
                            // changes and the other does not, the list will say a
                            // number the paper disagrees with.
                            QuestionCount = questions.Count(q =>
                                q.IsActive &&
                                (q.ExamId == exam.Id ||
                                 (q.ExamId == null &&
                                  q.CategoryId != null &&
                                  q.CategoryId == exam.CategoryId &&
                                  (q.LevelId == null || q.LevelId == exam.LevelId)))),
                            ShuffleQuestions = exam.ShuffleQuestions,
                            ShuffleOptions = exam.ShuffleOptions,
                            OneQuestionAtATime = exam.OneQuestionAtATime,
                            AllowBackNavigation = exam.AllowBackNavigation,
                            CollectIntegritySignals = exam.CollectIntegritySignals,
                            IsScheduled = exam.IsScheduled,
                            ScheduledStartTime = exam.ScheduledStartTime,
                            ScheduledEndTime = exam.ScheduledEndTime,
                            CreationTime = exam.CreationTime,
                        };

        var items = await projected.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync();

        return new PagedResultDto<ExamDto>(totalCount, items);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<ExamDto> GetAsync(Guid id)
    {
        var exam = await _exams.GetAsync(id);
        return await MapAsync(exam);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Create)]
    public async Task<ExamDto> CreateAsync(CreateUpdateExamDto input)
    {
        ValidateSchedule(input);

        var exam = new Exam(GuidGenerator.Create(), CurrentTenant.Id, input.Title, input.TimeLimitInMinutes);
        Apply(exam, input);

        await _exams.InsertAsync(exam, autoSave: true);

        return await MapAsync(exam);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<ExamDto> UpdateAsync(Guid id, CreateUpdateExamDto input)
    {
        ValidateSchedule(input);

        var exam = await _exams.GetAsync(id);
        Apply(exam, input);

        await _exams.UpdateAsync(exam, autoSave: true);

        return await MapAsync(exam);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        // Soft-deleted by ABP's convention, so results referencing this exam keep
        // resolving. A hard delete would orphan every attempt anyone ever sat.
        await _exams.DeleteAsync(id);
    }

    /// <summary>
    /// Reports everything that would prevent publishing, in one pass.
    /// <para>
    /// Publishing is the gate between a draft and something a real person sits, so
    /// the author gets the whole list rather than discovering the problems one
    /// refused click at a time.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<PublishCheckDto> CheckPublishAsync(Guid id)
    {
        var exam = await _exams.GetAsync(id);

        // Everything the exam can draw. Counting only what it owns refused an exam
        // whose whole paper comes from the shared bank, and the reason it gave
        // named a problem the author could not see: the questions were right
        // there in the bank listing.
        var bank = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(id, exam.CategoryId, exam.LevelId))
            .ToListAsync();

        var rules = await (await _blueprint.GetQueryableAsync())
            .Where(r => r.ExamId == id)
            .ToListAsync();

        var result = new PublishCheckDto
        {
            QuestionCount = bank.Count,
            TotalScore = bank.Sum(q => q.Score),
        };

        if (bank.Count == 0)
        {
            result.Blockers.Add(InternshipManagementSystemDomainErrorCodes.ExamHasNoQuestions);
        }

        if (rules.Count > 0)
        {
            result.FormLength = rules.Sum(r => r.QuestionCount);

            foreach (var rule in rules)
            {
                var available = bank.Count(q =>
                    (rule.TopicId is null || q.TopicId == rule.TopicId) &&
                    (rule.Difficulty is null || q.Difficulty == rule.Difficulty) &&
                    (rule.QuestionType is null || q.Type == rule.QuestionType));

                if (available < rule.QuestionCount)
                {
                    // A rule that cannot be filled quietly shortens everyone's paper,
                    // and the shortfall is invisible once candidates are sitting it.
                    result.Blockers.Add(InternshipManagementSystemDomainErrorCodes.ExamBlueprintUnsatisfiable);
                    break;
                }
            }
        }
        else
        {
            result.FormLength = exam.QuestionsPerForm ?? bank.Count;

            if (exam.QuestionsPerForm > bank.Count)
            {
                result.Blockers.Add(InternshipManagementSystemDomainErrorCodes.ExamFormLargerThanBank);
            }
        }

        // Warnings: the exam works, but the author probably did not intend this.

        if (bank.Count > 0 && bank.All(q => q.TopicId is null))
        {
            // Without topics there is no skill breakdown, so the result is a bare
            // number and nobody can act on it.
            result.Warnings.Add("IMS:Exam:NoTopicsAssigned");
        }

        if (exam.Mode == ExamMode.Practice && bank.Any(q => string.IsNullOrWhiteSpace(q.Explanation)))
        {
            // Practice mode without explanations is just an exam that shows the answer.
            result.Warnings.Add("IMS:Exam:PracticeWithoutExplanations");
        }

        if (rules.Count == 0 && exam.QuestionsPerForm is null && bank.Count > 0)
        {
            // Everyone gets the same paper, so one leak is everyone's paper.
            result.Warnings.Add("IMS:Exam:EveryoneGetsTheSameForm");
        }

        // A bank only slightly larger than the form draws nearly the same paper every
        // time, so the shuffling is theatre: two candidates overlap on almost every
        // question. Test-development practice puts the floor at roughly three times
        // the form length before rotation is worth anything.
        if (result.FormLength > 0 && bank.Count > 0 && bank.Count < result.FormLength * 3)
        {
            result.Warnings.Add("IMS:Exam:BankTooSmallToRotate");
        }

        // An item that has been in front of enough candidates has usually been
        // shared, and from then on it measures who has seen it rather than who
        // knows the answer.
        if (bank.Any(q => q.TimesServed >= OverExposedAfterServings))
        {
            result.Warnings.Add("IMS:Exam:BankHasOverExposedItems");
        }

        result.CanPublish = result.Blockers.Count == 0;

        return result;
    }

    /// <summary>
    /// Servings after which an item is treated as over-exposed. A round number
    /// standing in for a judgement that really depends on how public the audience
    /// is; a tenant will eventually want to set it themselves.
    /// </summary>
    private const int OverExposedAfterServings = 500;

    [Authorize(InternshipManagementSystemPermissions.Exams.Publish)]
    public async Task<ExamDto> PublishAsync(Guid id)
    {
        var check = await CheckPublishAsync(id);

        if (!check.CanPublish)
        {
            throw new BusinessException(check.Blockers.First());
        }

        var exam = await _exams.GetAsync(id);
        exam.Publish(check.QuestionCount);

        await _exams.UpdateAsync(exam, autoSave: true);

        return await MapAsync(exam);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Publish)]
    public async Task<ExamDto> ArchiveAsync(Guid id)
    {
        var exam = await _exams.GetAsync(id);

        // Archived stops new assignments; attempts already under way finish normally.
        exam.Status = ExamStatus.Archived;
        await _exams.UpdateAsync(exam, autoSave: true);

        return await MapAsync(exam);
    }

    // ---------------------------------------------------------------- blueprint

    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<List<BlueprintRuleDto>> GetBlueprintAsync(Guid examId)
    {
        var rules = await (await _blueprint.GetQueryableAsync())
            .Where(r => r.ExamId == examId)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();

        if (rules.Count == 0)
        {
            return [];
        }

        // The blueprint is checked against everything the exam can draw, so the
        // publish check and the delivery agree about what the bank holds.
        var exam = await _exams.GetAsync(examId);

        var bank = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(examId, exam.CategoryId, exam.LevelId))
            .Select(q => new { q.TopicId, q.Difficulty, q.Type })
            .ToListAsync();

        var topicIds = rules.Where(r => r.TopicId.HasValue).Select(r => r.TopicId!.Value).ToList();
        var topicNames = await (await _topics.GetQueryableAsync())
            .Where(t => topicIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        return rules.Select(rule => new BlueprintRuleDto
        {
            Id = rule.Id,
            TopicId = rule.TopicId,
            TopicName = rule.TopicId is { } tid && topicNames.TryGetValue(tid, out var name) ? name : null,
            Difficulty = rule.Difficulty,
            QuestionType = rule.QuestionType,
            QuestionCount = rule.QuestionCount,
            DisplayOrder = rule.DisplayOrder,
            // Shown next to the requested count so "draw 8 from a pool of 5" is
            // visible to the author, not to a candidate.
            AvailableCount = bank.Count(q =>
                (rule.TopicId is null || q.TopicId == rule.TopicId) &&
                (rule.Difficulty is null || q.Difficulty == rule.Difficulty) &&
                (rule.QuestionType is null || q.Type == rule.QuestionType)),
        }).ToList();
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<List<BlueprintRuleDto>> SetBlueprintAsync(Guid examId, List<CreateUpdateBlueprintRuleDto> rules)
    {
        var existing = await (await _blueprint.GetQueryableAsync())
            .Where(r => r.ExamId == examId)
            .ToListAsync();

        // Replaced wholesale: the blueprint is one recipe, and reconciling it rule by
        // rule invents ordering questions with no good answer.
        await _blueprint.DeleteManyAsync(existing, autoSave: true);

        var created = rules.Select((rule, index) =>
            new ExamBlueprintRule(GuidGenerator.Create(), CurrentTenant.Id, examId, rule.QuestionCount)
            {
                TopicId = rule.TopicId,
                Difficulty = rule.Difficulty,
                QuestionType = rule.QuestionType,
                DisplayOrder = rule.DisplayOrder == 0 ? index : rule.DisplayOrder,
            }).ToList();

        await _blueprint.InsertManyAsync(created, autoSave: true);

        return await GetBlueprintAsync(examId);
    }

    // ------------------------------------------------------------------ helpers

    private static void Apply(Exam exam, CreateUpdateExamDto input)
    {
        exam.Title = input.Title;
        exam.Description = input.Description;
        exam.CategoryId = input.CategoryId;
        exam.LevelId = input.LevelId;
        exam.Mode = input.Mode;
        exam.TimeLimitInMinutes = input.TimeLimitInMinutes;
        exam.PassingPercentage = input.PassingPercentage;
        exam.QuestionsPerForm = input.QuestionsPerForm;
        exam.ShuffleQuestions = input.ShuffleQuestions;
        exam.ShuffleOptions = input.ShuffleOptions;
        exam.OneQuestionAtATime = input.OneQuestionAtATime;
        exam.AllowBackNavigation = input.AllowBackNavigation;
        exam.CollectIntegritySignals = input.CollectIntegritySignals;
        exam.IsScheduled = input.IsScheduled;
        exam.ScheduledStartTime = input.ScheduledStartTime;
        exam.ScheduledEndTime = input.ScheduledEndTime;
    }

    private static void ValidateSchedule(CreateUpdateExamDto input)
    {
        if (!input.IsScheduled)
        {
            return;
        }

        if (input.ScheduledStartTime is null || input.ScheduledEndTime is null)
        {
            throw new BusinessException("IMS:Exam:ScheduleNeedsBothDates");
        }

        if (input.ScheduledStartTime >= input.ScheduledEndTime)
        {
            throw new BusinessException("IMS:Exam:ScheduleEndsBeforeItStarts");
        }
    }

    private async Task<ExamDto> MapAsync(Exam exam)
    {
        var questionCount = await (await _questions.GetQueryableAsync())
            .CountAsync(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId));

        var categoryName = exam.CategoryId is { } cid
            ? (await _categories.FindAsync(cid))?.Name
            : null;

        var levelName = exam.LevelId is { } lid
            ? (await _levels.FindAsync(lid))?.Name
            : null;

        return new ExamDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            CategoryId = exam.CategoryId,
            CategoryName = categoryName,
            LevelId = exam.LevelId,
            LevelName = levelName,
            Status = exam.Status,
            Mode = exam.Mode,
            TimeLimitInMinutes = exam.TimeLimitInMinutes,
            PassingPercentage = exam.PassingPercentage,
            QuestionsPerForm = exam.QuestionsPerForm,
            QuestionCount = questionCount,
            ShuffleQuestions = exam.ShuffleQuestions,
            ShuffleOptions = exam.ShuffleOptions,
            OneQuestionAtATime = exam.OneQuestionAtATime,
            AllowBackNavigation = exam.AllowBackNavigation,
            CollectIntegritySignals = exam.CollectIntegritySignals,
            IsScheduled = exam.IsScheduled,
            ScheduledStartTime = exam.ScheduledStartTime,
            ScheduledEndTime = exam.ScheduledEndTime,
            CreationTime = exam.CreationTime,
        };
    }
}
