using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// The question bank.
/// <para>
/// Guarded by <c>Questions.View</c> even to read, because a question carries its
/// answer key. This is the authoring surface; what a candidate receives is built
/// by <c>TakerQuestionProjector</c> and shares no type with it.
/// </para>
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Questions.Default)]
public class QuestionAppService : ApplicationService, IQuestionAppService
{
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<QuestionGroup, Guid> _groups;
    private readonly IRepository<Topic, Guid> _topics;
    private readonly QuestionPayloadValidator _validator;
    private readonly IGraderResolver _graders;

    public QuestionAppService(
        IRepository<Question, Guid> questions,
        IRepository<Exam, Guid> exams,
        IRepository<QuestionGroup, Guid> groups,
        IRepository<Topic, Guid> topics,
        QuestionPayloadValidator validator,
        IGraderResolver graders)
    {
        _questions = questions;
        _exams = exams;
        _groups = groups;
        _topics = topics;
        _validator = validator;
        _graders = graders;
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.View)]
    public async Task<PagedResultDto<QuestionDto>> GetListAsync(QuestionListRequestDto input)
    {
        var questions = await _questions.GetQueryableAsync();
        var topics = await _topics.GetQueryableAsync();

        var query = questions.AsQueryable();

        if (input.ExamId is { } examId)
        {
            // Everything this exam can draw: its own questions, plus the bank
            // questions its domain and level make available. Listing only the
            // owned ones would tell an author their bank is empty when it is not.
            var exam = await _exams.GetAsync(examId);

            query = query.Where(q =>
                q.ExamId == examId ||
                (q.ExamId == null &&
                 q.CategoryId != null &&
                 q.CategoryId == exam.CategoryId &&
                 (q.LevelId == null || q.LevelId == exam.LevelId)));
        }

        if (input.BankOnly == true)
        {
            query = query.Where(q => q.ExamId == null);
        }

        if (input.ExamSectionId is { } sectionId)
        {
            query = query.Where(q => q.ExamSectionId == sectionId);
        }

        if (input.CategoryId is { } categoryId)
        {
            query = query.Where(q => q.CategoryId == categoryId);
        }

        if (input.LevelId is { } levelId)
        {
            // A question with no level suits every level in its domain, so it belongs
            // in this result too.
            query = query.Where(q => q.LevelId == levelId || q.LevelId == null);
        }

        if (input.TopicId is { } topicId)
        {
            query = query.Where(q => q.TopicId == topicId);
        }

        if (!string.IsNullOrWhiteSpace(input.Type))
        {
            query = query.Where(q => q.Type == input.Type);
        }

        if (input.Difficulty is { } difficulty)
        {
            query = query.Where(q => q.Difficulty == difficulty);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            query = query.Where(q => q.Text.Contains(input.Filter));
        }

        var totalCount = await query.CountAsync();

        var projected = from question in query
                        join topic in topics on question.TopicId equals topic.Id into t
                        from topic in t.DefaultIfEmpty()
                        orderby question.DisplayOrder, question.CreationTime
                        select Project(question, topic);

        var items = await projected.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync();

        return new PagedResultDto<QuestionDto>(totalCount, items);
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.View)]
    public async Task<QuestionDto> GetAsync(Guid id)
    {
        var question = await _questions.GetAsync(id);
        var topic = question.TopicId is { } tid ? await _topics.FindAsync(tid) : null;

        return Project(question, topic);
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Create)]
    public async Task<QuestionDto> CreateAsync(CreateUpdateQuestionDto input)
    {
        // Checked here rather than at grading time: a payload the grader cannot read
        // becomes a question nobody can score, and that is discovered while somebody
        // is sitting the exam.
        var blockers = _validator.Blocking(input.Type, input.Payload);

        if (blockers.Count > 0)
        {
            throw new BusinessException(blockers[0]);
        }

        if (input.ExamId is null && input.CategoryId is null)
        {
            // Otherwise the question is owned by nothing: no exam can draw it and no
            // bank listing shows it, so it is written and then invisible.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.QuestionBelongsNowhere);
        }

        // Sanitised before the entity ever holds it, not only in Apply below.
        // Two assignments of the same field is how a reorder quietly reopens a hole.
        var question = new Question(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            input.ExamId,
            input.Type,
            RichTextSanitiser.Sanitise(input.Text));

        Apply(question, input);

        await _questions.InsertAsync(question, autoSave: true);

        return await GetAsync(question.Id);
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Edit)]
    public async Task<QuestionDto> UpdateAsync(Guid id, CreateUpdateQuestionDto input)
    {
        var blockers = _validator.Blocking(input.Type, input.Payload);

        if (blockers.Count > 0)
        {
            throw new BusinessException(blockers[0]);
        }

        var question = await _questions.GetAsync(id);
        question.Type = input.Type;
        Apply(question, input);

        await _questions.UpdateAsync(question, autoSave: true);

        return await GetAsync(question.Id);
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        // Soft delete keeps historic papers reproducible: an attempt's frozen form
        // references this question, and a disputed result has to be re-examinable.
        await _questions.DeleteAsync(id);
    }

    /// <summary>Non-blocking advice about a payload — what will need a human, and why.</summary>
    [Authorize(InternshipManagementSystemPermissions.Questions.View)]
    public Task<List<string>> ValidatePayloadAsync(string type, string payload) =>
        Task.FromResult(_validator.Validate(type, payload).ToList());

    /// <summary>
    /// The question types this server supports.
    /// <para>
    /// Served rather than hard-coded in Angular so the two cannot disagree about
    /// what exists. <c>IsAutoGraded</c> is answered by asking the resolver, so a
    /// type whose grader was never registered is described as human-graded — which
    /// is exactly how it will behave.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Questions.View)]
    public Task<List<QuestionTypeDescriptorDto>> GetTypesAsync()
    {
        var descriptors = Descriptors
            .Select(d => new QuestionTypeDescriptorDto
            {
                Type = d.Type,
                NameKey = $"::QuestionType:{d.Type}",
                DescriptionKey = $"::QuestionType:{d.Type}:Description",
                IsAutoGraded = IsAutoGraded(d),
                HasOptions = d.HasOptions,
                AcceptsUpload = d.AcceptsUpload,
                Icon = d.Icon,
            })
            .ToList();

        return Task.FromResult(descriptors);
    }

    // ------------------------------------------------------------------- groups

    [Authorize(InternshipManagementSystemPermissions.Questions.View)]
    public async Task<List<QuestionGroupDto>> GetGroupsAsync(Guid examId)
    {
        var groups = await (await _groups.GetQueryableAsync())
            .Where(g => g.ExamId == examId)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync();

        if (groups.Count == 0)
        {
            return [];
        }

        var groupIds = groups.Select(g => g.Id).ToList();

        var questions = await (await _questions.GetQueryableAsync())
            .Where(q => q.QuestionGroupId.HasValue && groupIds.Contains(q.QuestionGroupId.Value))
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        return groups.Select(g => new QuestionGroupDto
        {
            Id = g.Id,
            ExamId = g.ExamId,
            Instructions = g.Instructions,
            StimulusText = g.StimulusText,
            StimulusBlobName = g.StimulusBlobName,
            StimulusMediaType = g.StimulusMediaType,
            DisplayOrder = g.DisplayOrder,
            Questions = questions
                .Where(q => q.QuestionGroupId == g.Id)
                .Select(q => Project(q, null))
                .ToList(),
        }).ToList();
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Create)]
    [Authorize(InternshipManagementSystemPermissions.Questions.Edit)]
    public async Task<QuestionGroupDto> UpdateGroupAsync(Guid id, CreateUpdateQuestionGroupDto input)
    {
        var group = await _groups.GetAsync(id);

        group.Instructions = input.Instructions;
        group.StimulusText = input.StimulusText;
        group.StimulusBlobName = input.StimulusBlobName;
        group.StimulusMediaType = input.StimulusMediaType;
        group.DisplayOrder = input.DisplayOrder;

        await _groups.UpdateAsync(group, autoSave: true);

        return ToDto(group);
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Delete)]
    public async Task DeleteGroupAsync(Guid id)
    {
        // The questions under it are unhooked rather than deleted. Six questions
        // lost because the passage above them was wrong is the kind of damage
        // that makes people stop trusting a delete button — and they are still
        // perfectly good questions, just loose ones now.
        var attached = await (await _questions.GetQueryableAsync())
            .Where(q => q.QuestionGroupId == id)
            .ToListAsync();

        foreach (var question in attached)
        {
            question.QuestionGroupId = null;
        }

        await _questions.UpdateManyAsync(attached, autoSave: true);
        await _groups.DeleteAsync(id, autoSave: true);
    }

    public async Task<QuestionGroupDto> CreateGroupAsync(CreateUpdateQuestionGroupDto input)
    {
        var group = new QuestionGroup(GuidGenerator.Create(), CurrentTenant.Id, input.ExamId)
        {
            Instructions = input.Instructions,
            StimulusText = input.StimulusText,
            StimulusBlobName = input.StimulusBlobName,
            StimulusMediaType = input.StimulusMediaType,
            DisplayOrder = input.DisplayOrder,
        };

        await _groups.InsertAsync(group, autoSave: true);

        return ToDto(group);
    }

    private static QuestionGroupDto ToDto(QuestionGroup group) => new()
    {
        Id = group.Id,
        ExamId = group.ExamId,
        Instructions = group.Instructions,
        StimulusText = group.StimulusText,
        StimulusBlobName = group.StimulusBlobName,
        StimulusMediaType = group.StimulusMediaType,
        DisplayOrder = group.DisplayOrder,
    };

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Whether a machine scores this type.
    /// <para>
    /// Declared on the descriptor and then confirmed against the container: a type
    /// the descriptor calls automatic but which has no grader registered would be
    /// silently routed to the review queue, so it is reported as manual here and
    /// the authoring UI tells the truth.
    /// </para>
    /// </summary>
    private bool IsAutoGraded(Descriptor descriptor) =>
        descriptor.IsAutoGraded && _graders.Resolve(descriptor.Type) is not null;

    private static void Apply(Question question, CreateUpdateQuestionDto input)
    {
        // Sanitised here rather than at render time, so the stored value is the
        // safe one. Anything that reads a question later — an export, a
        // certificate, a client we have not written — gets what this produced.
        question.Text = RichTextSanitiser.Sanitise(input.Text);
        question.CategoryId = input.CategoryId;
        question.LevelId = input.LevelId;
        question.ExamSectionId = input.ExamSectionId;
        question.QuestionGroupId = input.QuestionGroupId;
        question.Payload = input.Payload;
        question.TopicId = input.TopicId;
        question.Difficulty = input.Difficulty;
        question.Score = input.Score;
        question.Explanation = RichTextSanitiser.Sanitise(input.Explanation);
        question.TimeLimitInSeconds = input.TimeLimitInSeconds;
        question.MediaBlobName = input.MediaBlobName;
        question.MediaType = input.MediaType;
        question.DisplayOrder = input.DisplayOrder;
        question.IsActive = input.IsActive;
    }

    private static QuestionDto Project(Question q, Topic? topic) => new()
    {
        Id = q.Id,
        ExamId = q.ExamId,
        CategoryId = q.CategoryId,
        LevelId = q.LevelId,
        ExamSectionId = q.ExamSectionId,
        QuestionGroupId = q.QuestionGroupId,
        Text = q.Text,
        Type = q.Type,
        Payload = q.Payload,
        TopicId = q.TopicId,
        TopicName = topic != null ? topic.Name : null,
        Difficulty = q.Difficulty,
        Score = q.Score,
        Explanation = q.Explanation,
        TimeLimitInSeconds = q.TimeLimitInSeconds,
        MediaBlobName = q.MediaBlobName,
        MediaType = q.MediaType,
        DisplayOrder = q.DisplayOrder,
        IsActive = q.IsActive,
        TimesAnswered = q.TimesAnswered,
        TimesServed = q.TimesServed,
        DifficultyIndex = q.DifficultyIndex,
        DiscriminationIndex = q.DiscriminationIndex,
        CreationTime = q.CreationTime,
    };

    private sealed record Descriptor(
        string Type,
        bool HasOptions,
        bool AcceptsUpload,
        string Icon,
        bool IsAutoGraded = false);

    /// <summary>
    /// Ordered as an author would reach for them: the everyday types first, the
    /// specialist ones after. A picker sorted alphabetically makes someone read
    /// thirteen entries to find "multiple choice".
    /// </summary>
    private static readonly Descriptor[] Descriptors =
    [
        new(QuestionTypes.SingleChoice, HasOptions: true, AcceptsUpload: false, Icon: "bi-ui-radios", IsAutoGraded: true),
        new(QuestionTypes.MultiSelect, HasOptions: true, AcceptsUpload: false, Icon: "bi-ui-checks", IsAutoGraded: true),
        new(QuestionTypes.TrueFalse, HasOptions: true, AcceptsUpload: false, Icon: "bi-toggle-on", IsAutoGraded: true),
        new(QuestionTypes.Text, HasOptions: false, AcceptsUpload: false, Icon: "bi-textarea-t"),
        new(QuestionTypes.Numeric, HasOptions: false, AcceptsUpload: false, Icon: "bi-123", IsAutoGraded: true),
        new(QuestionTypes.Matching, HasOptions: true, AcceptsUpload: false, Icon: "bi-arrow-left-right", IsAutoGraded: true),
        new(QuestionTypes.Ordering, HasOptions: true, AcceptsUpload: false, Icon: "bi-sort-numeric-down", IsAutoGraded: true),
        new(QuestionTypes.FillInTheBlank, HasOptions: false, AcceptsUpload: false, Icon: "bi-input-cursor-text", IsAutoGraded: true),
        new(QuestionTypes.Hotspot, HasOptions: false, AcceptsUpload: false, Icon: "bi-crosshair", IsAutoGraded: true),
        new(QuestionTypes.Code, HasOptions: false, AcceptsUpload: false, Icon: "bi-code-square", IsAutoGraded: true),
        new(QuestionTypes.FileUpload, HasOptions: false, AcceptsUpload: true, Icon: "bi-paperclip"),
        new(QuestionTypes.AudioResponse, HasOptions: false, AcceptsUpload: true, Icon: "bi-mic"),
        new(QuestionTypes.Scale, HasOptions: false, AcceptsUpload: false, Icon: "bi-sliders2"),
    ];
}
