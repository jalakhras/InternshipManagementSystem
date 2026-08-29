using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Assessment.Exams.Dtos;

/// <summary>
/// A question as its author sees it — payload and all.
/// <para>
/// This is the counterpart to <c>TakerQuestionDto</c>, and the difference between
/// them is the whole security model. This one carries the answer key and is
/// guarded by <c>Questions.View</c>; that one is stripped and goes to anyone with
/// a link. They are separate types rather than one type with conditional fields,
/// because a conditional field is a runtime decision and one day the condition is
/// written wrong.
/// </para>
/// </summary>
public class QuestionDto : AuditedEntityDto<Guid>
{
    /// <summary>Null when the question lives in the shared bank rather than one exam.</summary>
    public Guid? ExamId { get; set; }

    /// <summary>The domain that owns a bank question.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>The level or role a bank question is written for. Null suits every level.</summary>
    public Guid? LevelId { get; set; }
    public Guid? QuestionGroupId { get; set; }

    public string Text { get; set; } = default!;
    public string Type { get; set; } = default!;

    /// <summary>Type-specific data, including the key. Never leaves the authoring API.</summary>
    public string Payload { get; set; } = "{}";

    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }

    public QuestionDifficulty Difficulty { get; set; }
    public decimal Score { get; set; }

    public string? Explanation { get; set; }
    public int? TimeLimitInSeconds { get; set; }

    public string? MediaBlobName { get; set; }
    public string? MediaType { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    // Item analysis, accumulated from real attempts.

    public int TimesAnswered { get; set; }

    /// <summary>How many forms have carried this question. High counts mean it has circulated.</summary>
    public int TimesServed { get; set; }

    /// <summary>
    /// Share who answered correctly. Near 1 means the question separates nobody;
    /// near 0 usually means the question or its key is wrong.
    /// </summary>
    public decimal? DifficultyIndex { get; set; }

    /// <summary>
    /// Whether strong candidates outperform weak ones here. At or below zero the
    /// question measures the opposite of what it claims and should be pulled.
    /// </summary>
    public decimal? DiscriminationIndex { get; set; }
}

public class CreateUpdateQuestionDto
{
    /// <summary>The domain to file this question under when it is written into the bank.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>The level a bank question targets. Leave null for one that suits any level.</summary>
    public Guid? LevelId { get; set; }
    /// <summary>
    /// The exam that owns this question, or null when it goes into the shared bank.
    /// <para>
    /// Deliberately not <c>[Required]</c>. It was, and the attribute outlived the
    /// day the field became optional — so every attempt to write a bank question
    /// was refused by validation before the service saw it, and the shared bank
    /// could not be created through the API at all. The rule that actually applies
    /// is "belongs to an exam or to a domain", and the service enforces it.
    /// </para>
    /// </summary>
    public Guid? ExamId { get; set; }

    public Guid? QuestionGroupId { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Text { get; set; } = default!;

    /// <summary>One of <see cref="QuestionTypes"/>, or a type added later.</summary>
    [Required]
    [StringLength(64)]
    public string Type { get; set; } = default!;

    /// <summary>
    /// Type-specific JSON. Validated against the type on the server: a payload the
    /// grader cannot read would otherwise surface as a question nobody can score,
    /// discovered mid-exam.
    /// </summary>
    [Required]
    public string Payload { get; set; } = "{}";

    public Guid? TopicId { get; set; }

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

    [Range(0.01, 1000)]
    public decimal Score { get; set; } = 1m;

    [StringLength(4000)]
    public string? Explanation { get; set; }

    [Range(5, 7200)]
    public int? TimeLimitInSeconds { get; set; }

    [StringLength(256)]
    public string? MediaBlobName { get; set; }

    [StringLength(32)]
    public string? MediaType { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class QuestionListRequestDto : PagedAndSortedResultRequestDto
{
    public Guid? ExamId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? LevelId { get; set; }

    /// <summary>Restrict to questions in the shared bank, i.e. owned by no exam.</summary>
    public bool? BankOnly { get; set; }

    public Guid? TopicId { get; set; }
    public string? Type { get; set; }
    public QuestionDifficulty? Difficulty { get; set; }
    public string? Filter { get; set; }
}

/// <summary>A shared stimulus and the questions hanging off it.</summary>
public class QuestionGroupDto : AuditedEntityDto<Guid>
{
    public Guid ExamId { get; set; }

    public string? Instructions { get; set; }
    public string? StimulusText { get; set; }
    public string? StimulusBlobName { get; set; }
    public string? StimulusMediaType { get; set; }

    public int DisplayOrder { get; set; }

    public List<QuestionDto> Questions { get; set; } = new();
}

public class CreateUpdateQuestionGroupDto
{
    [Required]
    public Guid ExamId { get; set; }

    [StringLength(2048)]
    public string? Instructions { get; set; }

    /// <summary>A reading passage, a scenario, a case. Or use the blob for audio or a chart.</summary>
    public string? StimulusText { get; set; }

    [StringLength(256)]
    public string? StimulusBlobName { get; set; }

    [StringLength(32)]
    public string? StimulusMediaType { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>
/// Describes a question type to the authoring UI.
/// <para>
/// Served from the server rather than hard-coded in Angular so the two cannot
/// disagree about which types exist. A type with no grader registered is reported
/// here as human-graded, which is exactly how it will behave.
/// </para>
/// </summary>
public class QuestionTypeDescriptorDto
{
    public string Type { get; set; } = default!;

    /// <summary>Localisation key for the display name.</summary>
    public string NameKey { get; set; } = default!;

    /// <summary>Localisation key for the one-line explanation of when to use it.</summary>
    public string DescriptionKey { get; set; } = default!;

    /// <summary>Whether a machine can score it, or a person must.</summary>
    public bool IsAutoGraded { get; set; }

    /// <summary>Whether the type presents selectable options that can be shuffled.</summary>
    public bool HasOptions { get; set; }

    /// <summary>Whether the taker uploads a file or records audio.</summary>
    public bool AcceptsUpload { get; set; }

    /// <summary>Bootstrap Icons class for the type picker.</summary>
    public string Icon { get; set; } = default!;
}
