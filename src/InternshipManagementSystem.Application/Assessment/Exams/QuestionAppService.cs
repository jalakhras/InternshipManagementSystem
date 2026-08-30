using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private readonly QuestionCsvParser _csv;

    public QuestionAppService(
        IRepository<Question, Guid> questions,
        IRepository<Exam, Guid> exams,
        IRepository<QuestionGroup, Guid> groups,
        IRepository<Topic, Guid> topics,
        QuestionPayloadValidator validator,
        IGraderResolver graders,
        QuestionCsvParser csv)
    {
        _questions = questions;
        _exams = exams;
        _groups = groups;
        _topics = topics;
        _validator = validator;
        _graders = graders;
        _csv = csv;

        // The import template and every problem this service reports name a
        // column in the reader's language, so this service needs the resource.
        // ApplicationService leaves it unset, and an unset resource answers a key
        // with the key — which is how the author of an Arabic bank ends up
        // reading "QuestionImport:Column:Correct".
        LocalizationResource = typeof(Localization.InternshipManagementSystemResource);
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

        // What counted as correct before this edit, as the marker's screen would
        // render it. Compared against what counts as correct after, so a change
        // to the key is told apart from a change to the wording.
        var keyBefore = CorrectAnswerRenderer.Render(question.Type, question.Payload);

        question.Type = input.Type;
        Apply(question, input);

        var keyAfter = CorrectAnswerRenderer.Render(question.Type, question.Payload);

        if (!string.Equals(keyBefore, keyAfter, StringComparison.Ordinal))
        {
            ForgetStatistics(question);
        }

        await _questions.UpdateAsync(question, autoSave: true);

        return await GetAsync(question.Id);
    }

    /// <summary>
    /// Drops what was learned about a question whose key has changed.
    /// <para>
    /// DifficultyIndex is a lifetime running mean and was never reset, so an
    /// author who discovered a wrong key and fixed it inherited the wrong key's
    /// statistics — and the product went on reporting them as fact. The question
    /// would read "too hard" forever, because when the key was wrong almost
    /// everybody got it wrong, and nothing ever diluted that.
    /// </para>
    /// <para>
    /// That is the worst shape this defect could take: the author does the right
    /// thing, and the evidence that they did keeps arguing against them. Better
    /// to say "not measured yet" and mean it than to carry an average of two
    /// different questions.
    /// </para>
    /// <para>
    /// Only when the key changes. Fixing a typo in the wording is the same
    /// question and keeps its history; a candidate answering it was answering
    /// this question.
    /// </para>
    /// </summary>
    private static void ForgetStatistics(Question question)
    {
        question.DifficultyIndex = null;
        question.DiscriminationIndex = null;
        question.TimesAnswered = 0;
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

    // ------------------------------------------------------------------- import

    /// <summary>
    /// Reads a spreadsheet of questions.
    /// <para>
    /// Behind <c>Questions.Create</c> and nothing else: this writes questions,
    /// and it writes them where the caller could have written one by hand. The
    /// dry run sits behind the same permission on purpose — a preview reads a
    /// file the caller supplied and tells them what is in it, and letting
    /// somebody without the permission run it would be lending them a parser.
    /// </para>
    /// <para>
    /// One bad row never costs the good ones, which is the whole difference
    /// between an import somebody uses twice and one they abandon. Only a file
    /// with no readable headings is refused outright.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Questions.Create)]
    public async Task<ImportQuestionsResultDto> ImportAsync(ImportQuestionsDto input)
    {
        if (input.ExamId is null && input.CategoryId is null)
        {
            // The same rule CreateAsync enforces. A question owned by nothing is
            // written and then invisible: no exam draws it and no bank lists it.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.QuestionBelongsNowhere);
        }

        if (input.Content is null || input.Content.Length == 0)
        {
            throw new BusinessException("IMS:QuestionImport:FileEmpty");
        }

        if (input.Content.Length > MaxImportBytes)
        {
            throw new BusinessException("IMS:QuestionImport:FileTooLarge")
                .WithData("MaxMegabytes", MaxImportBytes / (1024 * 1024));
        }

        var sheet = _csv.Read(input.Content);
        var result = new ImportQuestionsResultDto();

        // What is already filed here, so importing a corrected sheet a second
        // time adds the six new questions rather than a second copy of the
        // seventy-four that were already there.
        var scope = await ScopeAsync(input);

        var existing = (await scope.Select(q => q.Text).ToListAsync())
            .Select(QuestionCsvParser.Normalise)
            .ToHashSet(StringComparer.Ordinal);

        var order = await scope.MaxAsync(q => (int?)q.DisplayOrder) ?? 0;

        var created = new List<Question>();

        foreach (var row in sheet.Rows)
        {
            if (row.Reason is { } reason)
            {
                result.Problems.Add(new ImportQuestionProblemDto
                {
                    Line = row.Line,
                    Column = row.Column ?? QuestionCsvParser.QuestionColumnKey,
                    Reason = reason,
                    Content = row.Content,
                });

                continue;
            }

            var draft = row.Question!;
            var text = RichTextSanitiser.Sanitise(draft.Text);
            var payload = QuestionCsvParser.PayloadFor(draft);

            // The same validator a hand-written question goes through. A row can
            // read cleanly and still describe a question no grader could score,
            // and finding that out mid-exam is the failure the validator exists
            // to prevent — an import must not be a way around it.
            var blockers = _validator.Blocking(draft.Type, payload);

            if (blockers.Count > 0)
            {
                result.Problems.Add(new ImportQuestionProblemDto
                {
                    Line = row.Line,
                    Column = QuestionCsvParser.CorrectColumnKey,
                    Reason = blockers[0],
                    Content = row.Content,
                });

                continue;
            }

            if (!existing.Add(QuestionCsvParser.Normalise(text)))
            {
                // Matched and left alone, whether it was already filed here or
                // simply appears twice in this file. Writing both would leave a
                // paper that can ask the same question twice.
                result.AlreadyPresent++;

                continue;
            }

            var question = new Question(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                input.ExamId,
                draft.Type,
                text)
            {
                // A question belongs to an exam or to a domain, never to both:
                // carrying a category on an exam's own question would put it in
                // the bank listing as well, where nobody meant to publish it.
                CategoryId = input.ExamId is null ? input.CategoryId : null,
                LevelId = input.ExamId is null ? input.LevelId : null,
                Payload = payload,
                Score = draft.Score,
                Difficulty = draft.Difficulty,
                Explanation = RichTextSanitiser.Sanitise(draft.Explanation),
                DisplayOrder = ++order,
            };

            created.Add(question);
            result.Created++;

            result.Preview.Add(new ImportQuestionPreviewDto
            {
                Line = row.Line,
                Text = text,
                Type = draft.Type,
                Score = draft.Score,
                Difficulty = draft.Difficulty,
                Options = draft.Options.Select(o => o.Text).ToList(),

                // Written out rather than numbered. The mistake worth catching
                // here is a key one row off, and a list of numbers looks exactly
                // as right when it is wrong.
                CorrectAnswers = draft.Type == QuestionTypes.FillInTheBlank
                    ? draft.AcceptedAnswers.ToList()
                    : draft.Options.Where(o => o.IsCorrect).Select(o => o.Text).ToList(),
            });
        }

        if (input.DryRun)
        {
            // Counted, previewed, nothing written.
            return result;
        }

        if (created.Count > 0)
        {
            await _questions.InsertManyAsync(created, autoSave: true);
        }

        return result;
    }

    /// <summary>
    /// The example spreadsheet.
    /// <para>
    /// Generated from the same column keys the parser matches against, so the
    /// file an author downloads is by construction one this server can read. The
    /// alternative — a help page listing headings to type by hand — goes stale
    /// silently and takes the import with it.
    /// </para>
    /// <para>
    /// The example rows are not decoration. They are how somebody learns that the
    /// correct answer may be a number, a list of numbers, or the answer written
    /// out, without first having to find prose that says so.
    /// </para>
    /// </summary>
    [Authorize(InternshipManagementSystemPermissions.Questions.Create)]
    public Task<string> GetImportTemplateAsync()
    {
        var csv = new StringBuilder();

        // A byte-order mark, because the next thing that happens to this file is
        // that somebody opens it in Excel — and without one every Arabic heading
        // arrives as mojibake, which makes the template look broken before the
        // author has typed anything.
        csv.Append('﻿');

        Row(csv,
            L[QuestionCsvParser.TypeColumnKey],
            L[QuestionCsvParser.QuestionColumnKey],
            L[QuestionCsvParser.OptionColumnKey, 1],
            L[QuestionCsvParser.OptionColumnKey, 2],
            L[QuestionCsvParser.OptionColumnKey, 3],
            L[QuestionCsvParser.OptionColumnKey, 4],
            L[QuestionCsvParser.CorrectColumnKey],
            L[QuestionCsvParser.MarksColumnKey],
            L[QuestionCsvParser.DifficultyColumnKey],
            L[QuestionCsvParser.ExplanationColumnKey]);

        // One row per supported type, in the order an author meets them. Between
        // them they demonstrate every way of naming the correct answer.
        foreach (var sample in new[] { "1", "2", "3", "4" })
        {
            var options = L[$"QuestionImport:Sample:{sample}:Options"].Value
                .Split('|', StringSplitOptions.TrimEntries);

            Row(csv,
                L[$"QuestionImport:Sample:{sample}:Type"],
                L[$"QuestionImport:Sample:{sample}:Question"],
                options.ElementAtOrDefault(0) ?? string.Empty,
                options.ElementAtOrDefault(1) ?? string.Empty,
                options.ElementAtOrDefault(2) ?? string.Empty,
                options.ElementAtOrDefault(3) ?? string.Empty,
                L[$"QuestionImport:Sample:{sample}:Correct"],
                L[$"QuestionImport:Sample:{sample}:Marks"],
                L[$"QuestionImport:Sample:{sample}:Difficulty"],
                L[$"QuestionImport:Sample:{sample}:Explanation"]);
        }

        return Task.FromResult(csv.ToString());
    }

    /// <summary>Everything already filed where this import is about to write.</summary>
    private async Task<IQueryable<Question>> ScopeAsync(ImportQuestionsDto input)
    {
        var questions = await _questions.GetQueryableAsync();

        if (input.ExamId is { } examId)
        {
            return questions.Where(q => q.ExamId == examId);
        }

        return questions.Where(q =>
            q.ExamId == null &&
            q.CategoryId == input.CategoryId &&
            q.LevelId == input.LevelId);
    }

    /// <summary>
    /// One row of the template, escaped.
    /// <para>
    /// Quoted whenever the value carries a separator, a quote or a line break —
    /// which a question written as prose very often does, and an unquoted one
    /// would arrive back here split across two columns.
    /// </para>
    /// </summary>
    private static void Row(StringBuilder csv, params object[] values)
    {
        csv.Append(string.Join(',', values.Select(value => Escape(value.ToString() ?? string.Empty))));
        csv.Append("\r\n");
    }

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r', ';']) < 0
            ? value
            : '"' + value.Replace("\"", "\"\"") + '"';

    /// <summary>
    /// The largest sheet this will read.
    /// <para>
    /// A question bank is text. Anything past this is somebody who picked the
    /// wrong file, and they are better told so than left waiting.
    /// </para>
    /// </summary>
    private const long MaxImportBytes = 2 * 1024 * 1024;

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

    [Authorize(InternshipManagementSystemPermissions.Questions.Create)]
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
        // A scale item has no right answer, so ScaleGrader always awards nothing
        // — deliberately; it is a survey question, reported rather than scored.
        // But the attempt's maximum is the sum of every question on the paper, so
        // marks put on one are added to what a candidate is measured against and
        // can never be earned back. Two marks on a scale item is two marks off
        // everybody, for answering a question that has no wrong answer.
        //
        // Refused rather than quietly zeroed: the author typed a number and is
        // owed the reason it cannot stand.
        if (input.Type == QuestionTypes.Scale)
        {
            if (input.Score != 0m)
            {
                throw new BusinessException(
                    InternshipManagementSystemDomainErrorCodes.QuestionScaleCarriesNoMarks);
            }
        }
        else if (input.Score <= 0m)
        {
            // The other half of the rule the attribute used to carry alone. A
            // question worth nothing is one a candidate can skip with no cost,
            // which is not something anybody means to put on a paper.
            throw new BusinessException(
                InternshipManagementSystemDomainErrorCodes.QuestionNeedsMarks);
        }

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
