using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// A reusable assessment: a bank of questions plus the rules for turning that bank
/// into one comparable form per taker.
/// </summary>
public class Exam : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>The tenant's own filing axis. See <c>Category</c>.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>How advanced, within the category.</summary>
    public Guid? LevelId { get; set; }

    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    /// <summary>Assessment withholds answers; Practice reveals them. See <see cref="ExamMode"/>.</summary>
    public ExamMode Mode { get; set; } = ExamMode.Assessment;

    /// <summary>Total time for the whole attempt.</summary>
    public int TimeLimitInMinutes { get; set; }

    /// <summary>
    /// Pass mark as a percentage of the form's maximum, not an absolute score.
    /// An absolute threshold is meaningless once forms differ in length.
    /// </summary>
    public decimal PassingPercentage { get; set; } = 60m;

    /// <summary>
    /// How many questions each taker receives. When it is smaller than the bank,
    /// every taker gets a different form drawn by <see cref="Blueprint"/>.
    /// Null means "the whole bank, in blueprint order".
    /// </summary>
    public int? QuestionsPerForm { get; set; }

    /// <summary>
    /// How a candidate's paper comes about: drawn for them as they start, one
    /// named form for everybody, or spread across the published forms.
    /// <para>
    /// The default is the behaviour that existed before forms did, so no exam
    /// changes by being loaded.
    /// </para>
    /// </summary>
    public ExamDeliveryMode DeliveryMode { get; set; } = ExamDeliveryMode.DrawPerCandidate;

    /// <summary>Which form everyone sits, when the mode is <see cref="ExamDeliveryMode.FixedForm"/>.</summary>
    public Guid? FixedFormId { get; set; }

    public ICollection<ExamSection> Sections { get; set; } = new List<ExamSection>();

    public ICollection<ExamForm> Forms { get; set; } = new List<ExamForm>();

    /// <summary>Randomise question order per attempt, seeded by attempt id so a reload is stable.</summary>
    public bool ShuffleQuestions { get; set; } = true;

    /// <summary>Randomise option order within a question, same seeding rule.</summary>
    public bool ShuffleOptions { get; set; } = true;

    /// <summary>Serve one question per request so the full paper is never in the browser at once.</summary>
    public bool OneQuestionAtATime { get; set; } = true;

    /// <summary>Let the taker move back to earlier questions. Off makes leakage harder.</summary>
    public bool AllowBackNavigation { get; set; } = true;

    /// <summary>Record paste, focus-loss and timing signals for the reviewer.</summary>
    public bool CollectIntegritySignals { get; set; } = true;

    /// <summary>Only open between these instants when set.</summary>
    public bool IsScheduled { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }

    public ICollection<QuestionGroup> QuestionGroups { get; set; } = new List<QuestionGroup>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<ExamBlueprintRule> Blueprint { get; set; } = new List<ExamBlueprintRule>();

    protected Exam() { }

    public Exam(Guid id, Guid? tenantId, string title, int timeLimitInMinutes) : base(id)
    {
        TenantId = tenantId;
        Title = title;
        TimeLimitInMinutes = timeLimitInMinutes;
    }

    /// <summary>
    /// Publishing is the gate between "being written" and "assignable"; everything
    /// downstream may assume a published exam can actually produce a form.
    /// </summary>
    public void Publish(int availableQuestionCount)
    {
        if (availableQuestionCount == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamHasNoQuestions);
        }

        if (QuestionsPerForm.HasValue && QuestionsPerForm.Value > availableQuestionCount)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormLargerThanBank);
        }

        Status = ExamStatus.Published;
    }

    /// <summary>
    /// Whether the exam is open at a moment given <b>in the organisation's own
    /// time zone</b>.
    /// <para>
    /// The scheduled window is wall-clock: a coordinator typed 09:00 into a
    /// date-and-time box meaning nine in the morning where they are. So the
    /// instant handed in has to be the same kind of thing, and callers convert
    /// before they call. Passing server-local time compares a Riyadh coordinator's
    /// nine o'clock against a container's nine o'clock in UTC, and the exam opens
    /// three hours late for a cohort sitting in a room waiting — which is exactly
    /// what the time-zone setting's own hint warns about, for a setting that
    /// until now nothing read.
    /// </para>
    /// </summary>
    public bool IsOpenAt(DateTime tenantLocalInstant)
    {
        if (Status != ExamStatus.Published)
        {
            return false;
        }

        if (!IsScheduled)
        {
            return true;
        }

        return tenantLocalInstant >= (ScheduledStartTime ?? DateTime.MinValue)
            && tenantLocalInstant <= (ScheduledEndTime ?? DateTime.MaxValue);
    }
}
