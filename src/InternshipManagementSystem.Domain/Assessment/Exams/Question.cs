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

    /// <summary>
    /// The exam that owns this question, or null when it lives in the shared bank.
    /// <para>
    /// A question written straight into one exam keeps an ExamId, which is the
    /// simple case and how most authoring starts. A bank question has none: it is
    /// owned by a <see cref="CategoryId"/> and <see cref="LevelId"/> instead, and
    /// every form for that level draws from it.
    /// </para>
    /// <para>
    /// This is what makes "three forms for A1" cost three blueprints rather than
    /// three copies of the same forty questions. Copies drift: a key corrected in
    /// one form stays wrong in the other two, and item statistics gathered against
    /// a copy describe a question nobody else is using.
    /// </para>
    /// </summary>
    public Guid? ExamId { get; set; }

    /// <summary>The domain this question belongs to when it lives in the shared bank.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// The level or role this question is written for. Null means it suits any level
    /// within its domain, which is common for questions measuring a foundation skill.
    /// </summary>
    public Guid? LevelId { get; set; }

    /// <summary>Set when this question belongs to a shared stimulus. See <see cref="QuestionGroup"/>.</summary>
    public Guid? QuestionGroupId { get; set; }

    /// <summary>
    /// The part of the exam this question belongs to — Listening, Grammar, and so
    /// on. Null on an exam that is not divided into sections, which is most of
    /// them.
    /// <para>
    /// Carried on the question as well as on its group because a section holds
    /// standalone questions too: a grammar section is thirty separate items with
    /// no shared passage between them.
    /// </para>
    /// </summary>
    public Guid? ExamSectionId { get; set; }

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
    /// How many forms this question has been served on. Distinct from
    /// <see cref="TimesAnswered"/>: exposure is how many candidates have seen it,
    /// which is what erodes a question's value once it circulates. A bank shared by
    /// many forms needs this to retire over-used items before they stop measuring.
    /// </summary>
    public int TimesServed { get; set; }

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

    public Question(Guid id, Guid? tenantId, Guid? examId, string type, string text) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
        Type = type;
        Text = text;
    }

    /// <summary>
    /// A question written into the shared bank rather than into one exam.
    /// </summary>
    public static Question InBank(Guid id, Guid? tenantId, Guid categoryId, Guid? levelId, string type, string text)
    {
        return new Question(id, tenantId, examId: null, type, text)
        {
            CategoryId = categoryId,
            LevelId = levelId,
        };
    }

    /// <summary>
    /// Whether this question is available to an exam in the given domain and level.
    /// <para>
    /// A bank question with no level suits every level in its domain; one with a
    /// level is offered only to exams at that level.
    /// </para>
    /// </summary>
    public bool IsDrawableBy(Guid examId, Guid? examCategoryId, Guid? examLevelId)
    {
        if (!IsActive)
        {
            return false;
        }

        if (ExamId == examId)
        {
            return true;
        }

        if (ExamId is not null || CategoryId is null || CategoryId != examCategoryId)
        {
            return false;
        }

        return LevelId is null || LevelId == examLevelId;
    }
}
