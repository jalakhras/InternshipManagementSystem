using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// One stimulus, several questions about it: a reading passage, a listening clip,
/// a price chart, a balance sheet. Without this the stimulus has to be repeated on
/// every question, the taker sees it several times, and there is no way to score
/// "how well did they read this passage".
/// <para>
/// Questions inside a group keep their order even when the exam shuffles — the
/// sequence usually carries meaning.
/// </para>
/// <para>
/// A group carries no section of its own. It did — <c>ExamSectionId</c>, dropped
/// by <c>Drop_Section_From_Passage</c> — and nothing ever wrote it, read it or
/// carried it on a DTO: no picker offered it, and <c>DrawBySection</c> pools on
/// the question's section alone, so a passage filed into Reading contributed
/// nothing at all. Two columns meaning "which part of the paper is this" with no
/// stated precedence between them is a disagreement waiting to happen and no
/// screen on which to see it. The question's own section is the one that is
/// authored, drawn from and frozen onto the delivered paper, so it is the only
/// one. File a passage's questions into the same section and the builder draws
/// them together: <c>Draw</c> takes whole blocks, so a passage is never
/// half-served.
/// </para>
/// </summary>
public class QuestionGroup : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    /// <summary>Shown above the questions: "Read the passage and answer questions 1-5".</summary>
    public string? Instructions { get; set; }

    /// <summary>The stimulus itself when it is text: a passage, a scenario, a case.</summary>
    public string? StimulusText { get; set; }

    /// <summary>Blob name of the stimulus when it is an image, audio or video file.</summary>
    public string? StimulusBlobName { get; set; }

    /// <summary>Media kind of <see cref="StimulusBlobName"/>: image, audio, video, document.</summary>
    public string? StimulusMediaType { get; set; }

    /// <summary>Position of this group within the exam.</summary>
    public int DisplayOrder { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    protected QuestionGroup() { }

    public QuestionGroup(Guid id, Guid? tenantId, Guid examId) : base(id)
    {
        TenantId = tenantId;
        ExamId = examId;
    }
}
