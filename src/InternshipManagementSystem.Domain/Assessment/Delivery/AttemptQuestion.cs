using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// A question as it appears on one taker's form: which question, in what position,
/// with options in what order.
/// <para>
/// The form is frozen here at start time rather than recomputed per request. That
/// keeps a reload stable, keeps the paper reproducible when a score is disputed,
/// and lets the exam bank be edited later without rewriting history.
/// </para>
/// </summary>
public class AttemptQuestion : AuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>Kept alongside the question so grouped questions stay together and in sequence.</summary>
    public Guid? QuestionGroupId { get; set; }

    /// <summary>Position on this taker's paper, zero-based.</summary>
    public int Position { get; set; }

    /// <summary>
    /// Option ids in the order this taker saw them, as a JSON array. Null when the
    /// type has no options or shuffling is off.
    /// </summary>
    public string? OptionOrder { get; set; }

    /// <summary>Marks this question carries, copied at form time so later edits do not move an old score.</summary>
    public decimal Score { get; set; }

    protected AttemptQuestion() { }

    public AttemptQuestion(Guid id, Guid? tenantId, Guid attemptId, Guid questionId, int position, decimal score) : base(id)
    {
        TenantId = tenantId;
        AttemptId = attemptId;
        QuestionId = questionId;
        Position = position;
        Score = score;
    }
}
