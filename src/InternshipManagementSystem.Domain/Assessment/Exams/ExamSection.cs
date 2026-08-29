using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// A named, separately scored part of an exam: Listening, Reading, Grammar,
/// Writing.
/// <para>
/// The language case is the one that proves it. A placement test that reports a
/// single percentage is useless, because a student strong in reading and weak in
/// listening needs a different class from the reverse — and one number cannot
/// tell them apart. Sections are what turn a result into something a coordinator
/// can act on.
/// </para>
/// <para>
/// It is not only a language idea. A recruiter's developer exam has a coding
/// part, a logic part and an English part, and "62%" hides which of the three
/// the candidate failed. A trading academy separates chart reading from risk
/// management for the same reason.
/// </para>
/// <para>
/// Timing lives here because a listening section is timed differently from an
/// essay: a recording runs for four minutes and the questions on it are answered
/// in six, while a writing task needs twenty and no clock ticking inside it.
/// </para>
/// </summary>
public class ExamSection : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>
    /// Read before the section begins — how many questions, whether audio plays
    /// once, whether the candidate can go back. A section is a small exam and
    /// deserves the same courtesy the exam gets.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// The competency this section measures, so a result reads as a profile
    /// rather than a list of headings. Optional: not every tenant keeps a
    /// competency tree.
    /// </summary>
    public Guid? TopicId { get; set; }

    /// <summary>
    /// Minutes for this section alone, or null to share the exam's clock.
    /// <para>
    /// When set, the section is closed when its own time runs out and the next
    /// one begins. This is what stops a candidate spending the whole hour on the
    /// essay and never reaching the listening.
    /// </para>
    /// </summary>
    public int? TimeLimitInMinutes { get; set; }

    /// <summary>
    /// A mark below which the whole exam fails however well the rest went, as a
    /// percentage of this section.
    /// <para>
    /// A safety-critical syllabus needs this: passing overall while failing the
    /// risk-management section is not a pass anyone should defend. Null means the
    /// section only contributes to the total.
    /// </para>
    /// </summary>
    public decimal? MinimumPercentage { get; set; }

    /// <summary>
    /// How many questions to draw here when the exam draws per candidate. Null
    /// takes everything the section holds.
    /// </summary>
    public int? QuestionsPerForm { get; set; }

    /// <summary>
    /// A gate rather than a measurement. Untimed, scored, and if it is failed the
    /// exam ends there.
    /// <para>
    /// "Have you completed Level 1?" "Do you hold a work permit?" A candidate who
    /// fails one is turned away in thirty seconds instead of after an hour, and
    /// no reviewer ever sees the attempt.
    /// </para>
    /// </summary>
    public bool IsQualifying { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<QuestionGroup> Groups { get; set; } = new List<QuestionGroup>();

    protected ExamSection() { }

    public ExamSection(Guid id, Guid? tenantId, Guid examId, string name, int displayOrder = 0) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        Name = name;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Whether a score in this section sinks the whole exam.
    /// </summary>
    public bool IsFailedAt(decimal percentage) =>
        MinimumPercentage is { } floor && percentage < floor;
}
