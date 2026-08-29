using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// One question. Everything specific to the question's type lives in
/// <see cref="Payload"/> rather than in columns, so a new type is a new grader and
/// a new payload shape — no migration, and no edit to existing types.
/// <para>
/// The old model had a column per type (OptionsJson, CodeStarterTemplate,
/// CodeExpectedOutput, CodeLanguage...). Six types cost six columns; the twelve
/// types a general assessment platform needs would have cost forty, nearly all
/// NULL on every row.
/// </para>
/// </summary>
public class Question : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    /// <summary>Set when this question belongs to a shared stimulus. See <see cref="QuestionGroup"/>.</summary>
    public Guid? QuestionGroupId { get; set; }

    /// <summary>The prompt shown to the taker.</summary>
    public string Text { get; set; } = default!;

    /// <summary>
    /// Which grader handles this question. A string, not an enum: see <see cref="QuestionTypes"/>.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Everything the type needs, as JSON. Options and their correctness for a
    /// choice question; pairs for matching; the accepted range for a numeric
    /// answer; the target region for a hotspot; expected output for code.
    /// <para>
    /// <b>This is answer-bearing.</b> It must never reach a taker's browser —
    /// only the sanitised projection built for delivery may.
    /// </para>
    /// </summary>
    public string Payload { get; set; } = "{}";

    /// <summary>The competency this question measures. Drives skill breakdown and the blueprint.</summary>
    public Guid? TopicId { get; set; }

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

    /// <summary>Marks available. Decimal because partial credit is a first-class case.</summary>
    public decimal Score { get; set; } = 1m;

    /// <summary>
    /// Why the correct answer is correct. Shown after submission in Practice mode —
    /// a learner who only sees a score has not learned anything.
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>Optional per-question timer. Short windows leave no room to consult an outside tool.</summary>
    public int? TimeLimitInSeconds { get; set; }

    /// <summary>Blob name of media attached to this question specifically.</summary>
    public string? MediaBlobName { get; set; }

    /// <summary>Media kind of <see cref="MediaBlobName"/>: image, audio, video, document.</summary>
    public string? MediaType { get; set; }

    /// <summary>Position within the exam, or within the group when grouped.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // ---- Item analysis, maintained from attempt data ----

    /// <summary>How many graded answers this question has accumulated.</summary>
    public int TimesAnswered { get; set; }

    /// <summary>
    /// Share of takers who got it right. Near 1 means the question separates nobody;
    /// near 0 usually means the question or its key is wrong.
    /// </summary>
    public decimal? DifficultyIndex { get; set; }

    /// <summary>
    /// Whether strong takers outperform weak ones on this question. At or below zero
    /// the question is measuring the opposite of what it claims, and should be pulled.
    /// </summary>
    public decimal? DiscriminationIndex { get; set; }

    protected Question() { }

    public Question(Guid id, Guid? tenantId, Guid examId, string type, string text) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        Type = type;
        Text = text;
    }
}
