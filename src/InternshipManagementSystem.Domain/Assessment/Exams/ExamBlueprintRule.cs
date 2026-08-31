using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// One line of the recipe for building a form: "8 medium questions from Listening".
/// <para>
/// This is what makes multiple forms fair. Draw 30 questions from a bank of 120 at
/// random and two takers get papers of different difficulty, so their scores cannot
/// be compared. Draw them rule by rule and every form covers the same topics at the
/// same difficulty mix — different questions, same measurement. A leaked paper is
/// then worth nothing, without any surveillance.
/// </para>
/// </summary>
public class ExamBlueprintRule : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    /// <summary>
    /// The section this rule fills, or null to draw from the whole exam. A
    /// sectioned exam composes its paper section by section, because "twenty
    /// questions across four skills" is not the same paper as "five of each".
    /// </summary>
    public Guid? ExamSectionId { get; set; }

    /// <summary>Draw from this competency. Null draws from any.</summary>
    public Guid? TopicId { get; set; }

    /// <summary>Draw at this difficulty. Null draws from any.</summary>
    public QuestionDifficulty? Difficulty { get; set; }

    /// <summary>Draw only this type. Null draws from any.</summary>
    public string? QuestionType { get; set; }

    /// <summary>How many questions this rule contributes.</summary>
    public int QuestionCount { get; set; }

    /// <summary>Order in which rules are applied, and the order their questions appear.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this rule would accept that question.
    /// <para>
    /// One definition, because the builder, the publish check and the count shown
    /// beside the rule all ask it. Written out three times they drift, and the
    /// drift that matters is a publish check that approves a paper the builder
    /// then cannot fill.
    /// </para>
    /// </summary>
    public bool Matches(Question question) =>
        question.IsActive
        && (TopicId is null || question.TopicId == TopicId)
        && (Difficulty is null || question.Difficulty == Difficulty)
        && (QuestionType is null
            || string.Equals(question.Type, QuestionType, StringComparison.OrdinalIgnoreCase));

    protected ExamBlueprintRule() { }

    public ExamBlueprintRule(Guid id, Guid? tenantId, Guid examId, int questionCount) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        QuestionCount = questionCount;
    }
}
