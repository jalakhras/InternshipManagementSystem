using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams.Dtos;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// The shape of an exam: its sections, and the named papers built from them.
/// <para>
/// Guarded by the exam permissions rather than the question ones. Deciding an
/// exam has four skills, or that Form 2 is ready to sit, is an act on the exam —
/// the questions themselves are untouched by everything here.
/// </para>
/// </summary>
[Authorize(InternshipManagementSystemPermissions.Exams.Default)]
public class ExamStructureAppService : ApplicationService, IExamStructureAppService
{
    private readonly IRepository<Exam, Guid> _exams;
    private readonly IRepository<ExamSection, Guid> _sections;
    private readonly IRepository<ExamForm, Guid> _forms;
    private readonly IRepository<ExamFormQuestion, Guid> _formQuestions;
    private readonly IRepository<Question, Guid> _questions;
    private readonly IRepository<ExamBlueprintRule, Guid> _blueprint;
    private readonly IRepository<Topic, Guid> _topics;
    private readonly ExamFormBuilder _builder;

    public ExamStructureAppService(
        IRepository<Exam, Guid> exams,
        IRepository<ExamSection, Guid> sections,
        IRepository<ExamForm, Guid> forms,
        IRepository<ExamFormQuestion, Guid> formQuestions,
        IRepository<Question, Guid> questions,
        IRepository<ExamBlueprintRule, Guid> blueprint,
        IRepository<Topic, Guid> topics,
        ExamFormBuilder builder)
    {
        _exams = exams;
        _sections = sections;
        _forms = forms;
        _formQuestions = formQuestions;
        _questions = questions;
        _blueprint = blueprint;
        _topics = topics;
        _builder = builder;
    }

    // --------------------------------------------------------------- sections

    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<List<ExamSectionDto>> GetSectionsAsync(Guid examId)
    {
        var sections = await (await _sections.GetQueryableAsync())
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        if (sections.Count == 0)
        {
            return [];
        }

        var exam = await _exams.GetAsync(examId);

        // Counted per section so an author can see, before publishing, whether a
        // section can fill itself. A listening section wanting eight questions
        // from a pool of three is a paper that quietly comes out short.
        var counts = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(examId, exam.CategoryId, exam.LevelId))
            .Where(q => q.ExamSectionId != null)
            .GroupBy(q => q.ExamSectionId!.Value)
            .Select(g => new { SectionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SectionId, x => x.Count);

        var topicIds = sections.Where(s => s.TopicId.HasValue).Select(s => s.TopicId!.Value).ToList();

        var topics = topicIds.Count == 0
            ? []
            : await (await _topics.GetQueryableAsync())
                .Where(t => topicIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name);

        return sections.Select(section => Project(section, counts, topics)).ToList();
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<ExamSectionDto> CreateSectionAsync(CreateUpdateExamSectionDto input)
    {
        await RequireOwnExamAsync(input.ExamId);

        var section = new ExamSection(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            input.ExamId,
            input.Name,
            input.DisplayOrder);

        Apply(section, input);

        await _sections.InsertAsync(section, autoSave: true);

        return Project(section, new Dictionary<Guid, int>(), new Dictionary<Guid, string>());
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<ExamSectionDto> UpdateSectionAsync(Guid id, CreateUpdateExamSectionDto input)
    {
        var section = await _sections.GetAsync(id);

        Apply(section, input);

        await _sections.UpdateAsync(section, autoSave: true);

        return (await GetSectionsAsync(section.ExamId)).First(s => s.Id == id);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task DeleteSectionAsync(Guid id)
    {
        var section = await _sections.GetAsync(id);

        // Questions outlive the section they sat in. Deleting a heading must not
        // delete a term's worth of authoring with it, so they fall back to the
        // exam and an author decides where they belong.
        var orphans = await (await _questions.GetQueryableAsync())
            .Where(q => q.ExamSectionId == id)
            .ToListAsync();

        foreach (var question in orphans)
        {
            question.ExamSectionId = null;
        }

        if (orphans.Count > 0)
        {
            await _questions.UpdateManyAsync(orphans, autoSave: false);
        }

        await _sections.DeleteAsync(section, autoSave: true);
    }

    // ------------------------------------------------------------------ forms

    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<List<ExamFormDto>> GetFormsAsync(Guid examId)
    {
        var forms = await (await _forms.GetQueryableAsync())
            .Where(f => f.ExamId == examId)
            .OrderBy(f => f.Code)
            .ToListAsync();

        if (forms.Count == 0)
        {
            return [];
        }

        var ids = forms.Select(f => f.Id).ToList();

        var counts = await (await _formQuestions.GetQueryableAsync())
            .Where(q => ids.Contains(q.ExamFormId))
            .GroupBy(q => q.ExamFormId)
            .Select(g => new { FormId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FormId, x => x.Count);

        return forms.Select(form => Project(form, counts.GetValueOrDefault(form.Id))).ToList();
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.View)]
    public async Task<ExamFormDetailDto> GetFormAsync(Guid id)
    {
        var form = await _forms.GetAsync(id);
        var slots = await LoadSlotsAsync(id);

        var questionIds = slots.Select(s => s.QuestionId).ToList();

        var questions = await (await _questions.GetQueryableAsync())
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id);

        var detail = new ExamFormDetailDto
        {
            Id = form.Id,
            ExamId = form.ExamId,
            Name = form.Name,
            Code = form.Code,
            Status = form.Status,
            WasGenerated = form.WasGenerated,
            TimesUsed = form.TimesUsed,
            MaxScore = form.MaxScore,
            QuestionCount = slots.Count,
        };

        foreach (var slot in slots)
        {
            // A slot whose question has since been deleted is skipped rather than
            // shown as a blank row: the form is a draft until published, and an
            // author regenerating it is the right answer.
            if (!questions.TryGetValue(slot.QuestionId, out var question))
            {
                continue;
            }

            detail.Questions.Add(new ExamFormQuestionDto
            {
                QuestionId = question.Id,
                Text = question.Text,
                Type = question.Type,
                Difficulty = question.Difficulty,
                DisplayOrder = slot.DisplayOrder,
                Score = slot.Score,
            });
        }

        return detail;
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<ExamFormDto> CreateFormAsync(CreateUpdateExamFormDto input)
    {
        await RequireOwnExamAsync(input.ExamId);

        var taken = await (await _forms.GetQueryableAsync())
            .AnyAsync(f => f.ExamId == input.ExamId && f.Code == input.Code);

        if (taken)
        {
            // A code identifies a form on a result sheet, so two sharing one is a
            // result nobody can trace back to a paper.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormCodeTaken);
        }

        var form = new ExamForm(GuidGenerator.Create(), CurrentTenant.Id, input.ExamId, input.Name, input.Code);

        await _forms.InsertAsync(form, autoSave: true);

        return Project(form, 0);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<ExamFormDetailDto> GenerateFormAsync(Guid id, GenerateExamFormDto input)
    {
        var form = await RequireDraftAsync(id);
        var exam = await _exams.GetAsync(form.ExamId);

        exam.Blueprint = await (await _blueprint.GetQueryableAsync())
            .Where(r => r.ExamId == exam.Id)
            .ToListAsync();

        var bank = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId))
            .ToListAsync();

        if (bank.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamHasNoQuestions);
        }

        // The same builder the delivery uses, so a generated form is a paper a
        // candidate could actually have been given. A separate selection here
        // would eventually disagree with the one that matters.
        var seed = input.Seed ?? Random.Shared.Next();
        var drawn = _builder.Build(exam, bank, Guid.Empty, CurrentTenant.Id, seed);

        await ReplaceSlotsAsync(form, drawn.Select(slot => (slot.QuestionId, slot.Score)).ToList());

        form.WasGenerated = true;
        await _forms.UpdateAsync(form, autoSave: true);

        return await GetFormAsync(id);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Edit)]
    public async Task<ExamFormDetailDto> SetFormQuestionsAsync(Guid id, SetExamFormQuestionsDto input)
    {
        var form = await RequireDraftAsync(id);
        var exam = await _exams.GetAsync(form.ExamId);

        var chosen = await (await _questions.GetQueryableAsync())
            .Where(Question.DrawableBy(exam.Id, exam.CategoryId, exam.LevelId))
            .Where(q => input.QuestionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id);

        var missing = input.QuestionIds.Where(qid => !chosen.ContainsKey(qid)).ToList();

        if (missing.Count > 0)
        {
            // A question this exam cannot draw is one from another domain or level,
            // and putting it on the paper would be the cross-tenant leak in
            // miniature.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormQuestionNotAvailable);
        }

        // The caller's order is the paper's order.
        await ReplaceSlotsAsync(form, input.QuestionIds.Select(qid => (qid, chosen[qid].Score)).ToList());

        form.WasGenerated = false;
        await _forms.UpdateAsync(form, autoSave: true);

        return await GetFormAsync(id);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Publish)]
    public async Task<ExamFormDto> PublishFormAsync(Guid id)
    {
        var form = await _forms.GetAsync(id);

        form.Questions = await LoadSlotsAsync(id);

        // Publish enforces the two rules that cannot be fixed afterwards without
        // invalidating results already earned.
        form.Publish(form.Questions.Sum(q => q.Score));

        await _forms.UpdateAsync(form, autoSave: true);

        return Project(form, form.Questions.Count);
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Publish)]
    public async Task<ExamFormDto> RetireFormAsync(Guid id)
    {
        var form = await _forms.GetAsync(id);

        form.Retire();
        await _forms.UpdateAsync(form, autoSave: true);

        return Project(form, await CountSlotsAsync(id));
    }

    [Authorize(InternshipManagementSystemPermissions.Exams.Delete)]
    public async Task DeleteFormAsync(Guid id)
    {
        var form = await _forms.GetAsync(id);

        if (form.TimesUsed > 0)
        {
            // Somebody sat it. Deleting it would leave their result pointing at a
            // paper that no longer exists, which is the one thing a result must
            // never do.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormAlreadyUsed);
        }

        await _forms.DeleteAsync(form, autoSave: true);
    }

    // ---------------------------------------------------------------- helpers

    private async Task RequireOwnExamAsync(Guid examId)
    {
        // Resolved through the tenant-filtered repository, so an id learned from
        // elsewhere cannot attach a section or a form to another centre's exam.
        await _exams.GetAsync(examId);
    }

    private async Task<ExamForm> RequireDraftAsync(Guid id)
    {
        var form = await _forms.GetAsync(id);

        if (form.Status != ExamFormStatus.Draft)
        {
            // Editing a published form means two candidates sat "Form 2" and
            // answered different papers, which removes the only thing a form was
            // for.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormNotEditable);
        }

        return form;
    }

    private async Task<List<ExamFormQuestion>> LoadSlotsAsync(Guid formId) =>
        await (await _formQuestions.GetQueryableAsync())
            .Where(q => q.ExamFormId == formId)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

    private async Task<int> CountSlotsAsync(Guid formId) =>
        await (await _formQuestions.GetQueryableAsync()).CountAsync(q => q.ExamFormId == formId);

    private async Task ReplaceSlotsAsync(ExamForm form, List<(Guid QuestionId, decimal Score)> chosen)
    {
        var existing = await LoadSlotsAsync(form.Id);

        if (existing.Count > 0)
        {
            await _formQuestions.DeleteManyAsync(existing, autoSave: true);
        }

        var slots = chosen
            .Select((item, index) => new ExamFormQuestion(
                GuidGenerator.Create(), CurrentTenant.Id, form.Id, item.QuestionId, index, item.Score))
            .ToList();

        if (slots.Count > 0)
        {
            await _formQuestions.InsertManyAsync(slots, autoSave: true);
        }
    }

    private static void Apply(ExamSection section, CreateUpdateExamSectionDto input)
    {
        section.Name = input.Name;
        section.Instructions = input.Instructions;
        section.TopicId = input.TopicId;
        section.TimeLimitInMinutes = input.TimeLimitInMinutes;
        section.MinimumPercentage = input.MinimumPercentage;
        section.QuestionsPerForm = input.QuestionsPerForm;
        section.IsQualifying = input.IsQualifying;
        section.DisplayOrder = input.DisplayOrder;
    }

    private static ExamSectionDto Project(
        ExamSection section,
        IReadOnlyDictionary<Guid, int> counts,
        IReadOnlyDictionary<Guid, string> topics) => new()
    {
        Id = section.Id,
        ExamId = section.ExamId,
        Name = section.Name,
        Instructions = section.Instructions,
        TopicId = section.TopicId,
        TopicName = section.TopicId is { } id && topics.TryGetValue(id, out var name) ? name : null,
        TimeLimitInMinutes = section.TimeLimitInMinutes,
        MinimumPercentage = section.MinimumPercentage,
        QuestionsPerForm = section.QuestionsPerForm,
        IsQualifying = section.IsQualifying,
        DisplayOrder = section.DisplayOrder,
        QuestionCount = counts.GetValueOrDefault(section.Id),
        CreationTime = section.CreationTime,
    };

    private static ExamFormDto Project(ExamForm form, int questionCount) => new()
    {
        Id = form.Id,
        ExamId = form.ExamId,
        Name = form.Name,
        Code = form.Code,
        Status = form.Status,
        WasGenerated = form.WasGenerated,
        TimesUsed = form.TimesUsed,
        MaxScore = form.MaxScore,
        QuestionCount = questionCount,
        CreationTime = form.CreationTime,
    };
}
