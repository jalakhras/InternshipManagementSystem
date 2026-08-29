using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// One named, fixed version of an exam — Form 1, Form 2, the November paper.
/// <para>
/// Distinct from the per-attempt draw the blueprint already makes. That draw is
/// ephemeral: it exists for one candidate, nobody reviews it, and no two
/// candidates sat the same one. A form is the opposite of all three, and each
/// property is there because somebody needs it.
/// </para>
/// <para>
/// <b>It can be reviewed.</b> A certification body cannot ship a paper no human
/// has read. A random draw cannot be read before it exists.
/// </para>
/// <para>
/// <b>A retake can differ.</b> "Sit it again" must not mean a redraw that
/// happens to repeat half the questions. Assigning Form 2 to the second attempt
/// is a guarantee rather than a probability.
/// </para>
/// <para>
/// <b>Sittings can be separated.</b> The morning group takes Form 1 and the
/// afternoon Form 2, so what leaks at lunchtime is worth nothing after it.
/// </para>
/// <para>
/// <b>Scores can be compared.</b> Two candidates' results only mean the same
/// thing if the papers behind them are known. A form has a fixed difficulty that
/// can be measured; a draw has a different one every time.
/// </para>
/// </summary>
public class ExamForm : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    /// <summary>
    /// What the people running the exam call it. Shown to staff and printed on
    /// the result, never shown to a candidate — knowing you sat Form 2 tells you
    /// there is a Form 1 to go and find.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// A short code for the result sheet and for equating studies later. Unique
    /// within its exam.
    /// </summary>
    public string Code { get; set; } = default!;

    public ExamFormStatus Status { get; set; } = ExamFormStatus.Draft;

    /// <summary>
    /// Set when the form was generated from the exam's blueprint rather than
    /// picked by hand, so a later reviewer can tell which it was.
    /// </summary>
    public bool WasGenerated { get; set; }

    /// <summary>
    /// How many attempts have been sat on this form. The reason a bank needs more
    /// than one form: exposure accrues per form, and a form that has been in front
    /// of enough people has circulated whatever its questions' individual counts say.
    /// </summary>
    public int TimesUsed { get; set; }

    /// <summary>
    /// Total marks, frozen when the form is published.
    /// <para>
    /// Stored rather than summed on demand because a question's marks can be
    /// edited afterwards, and a result must keep meaning what it meant on the day
    /// it was earned.
    /// </para>
    /// </summary>
    public decimal MaxScore { get; set; }

    public ICollection<ExamFormQuestion> Questions { get; set; } = new List<ExamFormQuestion>();

    protected ExamForm() { }

    public ExamForm(Guid id, Guid? tenantId, Guid examId, string name, string code) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        Name = name;
        Code = code;
    }

    /// <summary>
    /// Freezes the form for use.
    /// <para>
    /// After this the question list does not change. Editing a live form would
    /// mean two candidates sat "Form 2" and answered different papers, which
    /// takes away the only thing a form was for.
    /// </para>
    /// </summary>
    public void Publish(decimal maxScore)
    {
        if (Questions.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormHasNoQuestions);
        }

        if (Questions.Select(q => q.QuestionId).Distinct().Count() != Questions.Count)
        {
            // A duplicated question is scored twice and read twice, and the taker
            // reasonably concludes the exam is broken.
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamFormHasDuplicateQuestions);
        }

        MaxScore = maxScore;
        Status = ExamFormStatus.Published;
    }

    /// <summary>
    /// Takes the form out of rotation without deleting it, so results that
    /// reference it keep resolving and an equating study can still read it.
    /// </summary>
    public void Retire() => Status = ExamFormStatus.Retired;

    public bool IsUsable => Status == ExamFormStatus.Published;
}

/// <summary>One question's place on one form.</summary>
public class ExamFormQuestion : AuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamFormId { get; set; }

    public Guid QuestionId { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// The marks this question carries on this form, copied when the form is
    /// built.
    /// <para>
    /// Copied rather than read through, for the same reason the attempt copies
    /// them: an author raising a question from 2 marks to 5 next month must not
    /// change what a candidate scored last month.
    /// </para>
    /// </summary>
    public decimal Score { get; set; }

    protected ExamFormQuestion() { }

    public ExamFormQuestion(Guid id, Guid? tenantId, Guid examFormId, Guid questionId, int displayOrder, decimal score)
        : base(id)
    {
        TenantId = tenantId;
        ExamFormId = examFormId;
        QuestionId = questionId;
        DisplayOrder = displayOrder;
        Score = score;
    }
}

public enum ExamFormStatus
{
    Draft = 0,
    Published = 1,
    Retired = 2,
}

/// <summary>
/// How an exam decides which questions a particular candidate sees.
/// </summary>
public enum ExamDeliveryMode
{
    /// <summary>
    /// The blueprint draws for each candidate as they start. Nobody reviews the
    /// result and no two candidates sit the same paper. Cheapest to run, and the
    /// only sensible choice for practice.
    /// </summary>
    DrawPerCandidate = 0,

    /// <summary>
    /// Everyone on this exam sits one named form. What a certification body does,
    /// because the paper has to be approved before anyone sees it.
    /// </summary>
    FixedForm = 1,

    /// <summary>
    /// Candidates are spread across the published forms in turn. Keeps the review
    /// guarantee while making a leaked paper worth a fraction of the sitting.
    /// </summary>
    RotateForms = 2,
}
