using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using InternshipManagementSystem.Assessment;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Assessment.Exams.Dtos;

// ---------------------------------------------------------------- sections

public class ExamSectionDto : AuditedEntityDto<Guid>
{
    public Guid ExamId { get; set; }

    public string Name { get; set; } = default!;

    public string? Instructions { get; set; }

    public Guid? TopicId { get; set; }

    public string? TopicName { get; set; }

    public int? TimeLimitInMinutes { get; set; }

    public decimal? MinimumPercentage { get; set; }

    public int? QuestionsPerForm { get; set; }

    public bool IsQualifying { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>How many questions this section can draw on, so an author can see whether it can fill itself.</summary>
    public int QuestionCount { get; set; }
}

public class CreateUpdateExamSectionDto
{
    [Required]
    public Guid ExamId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [StringLength(2048)]
    public string? Instructions { get; set; }

    public Guid? TopicId { get; set; }

    [Range(1, 600)]
    public int? TimeLimitInMinutes { get; set; }

    /// <summary>
    /// A floor below which the whole exam fails however well the rest went.
    /// </summary>
    [Range(1, 100)]
    public decimal? MinimumPercentage { get; set; }

    [Range(1, 500)]
    public int? QuestionsPerForm { get; set; }

    public bool IsQualifying { get; set; }

    public int DisplayOrder { get; set; }
}

// ------------------------------------------------------------------- forms

public class ExamFormDto : AuditedEntityDto<Guid>
{
    public Guid ExamId { get; set; }

    public string Name { get; set; } = default!;

    public string Code { get; set; } = default!;

    public ExamFormStatus Status { get; set; }

    public bool WasGenerated { get; set; }

    public int TimesUsed { get; set; }

    public decimal MaxScore { get; set; }

    public int QuestionCount { get; set; }
}

public class ExamFormDetailDto : ExamFormDto
{
    public List<ExamFormQuestionDto> Questions { get; set; } = new();
}

public class ExamFormQuestionDto
{
    public Guid QuestionId { get; set; }

    /// <summary>The prompt, so a reviewer can read the paper rather than a list of identifiers.</summary>
    public string Text { get; set; } = default!;

    public string Type { get; set; } = default!;

    public QuestionDifficulty Difficulty { get; set; }

    public int DisplayOrder { get; set; }

    public decimal Score { get; set; }
}

public class CreateUpdateExamFormDto
{
    [Required]
    public Guid ExamId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Code { get; set; } = default!;
}

/// <summary>
/// The questions on a form, in the order they will be asked.
/// </summary>
public class SetExamFormQuestionsDto
{
    [Required]
    public List<Guid> QuestionIds { get; set; } = new();
}

/// <summary>
/// Asks the blueprint to fill a form, so an author starts from a paper rather
/// than from an empty list.
/// </summary>
public class GenerateExamFormDto
{
    /// <summary>
    /// Optional. Two generations with the same seed produce the same paper, which
    /// is what lets a form be regenerated after an edit without becoming an
    /// entirely different exam.
    /// </summary>
    public int? Seed { get; set; }
}
