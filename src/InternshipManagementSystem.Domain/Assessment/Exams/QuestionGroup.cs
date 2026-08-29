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
/// </summary>
public class QuestionGroup : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ExamId { get; set; }

    /// <summary>
    /// The section this stimulus belongs to, or null on an exam with no sections.
    /// <para>
    /// A reading passage belongs in Reading and an audio clip in Listening. When
    /// a section draws a form it takes whole groups, never part of one — half a
    /// passage's questions is a passage the candidate read for nothing.
    /// </para>
    /// </summary>
    public Guid? ExamSectionId { get; set; }

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
