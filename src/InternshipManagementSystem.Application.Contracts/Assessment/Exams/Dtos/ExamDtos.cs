using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Assessment.Exams.Dtos;

/// <summary>An exam as the authoring screens see it.</summary>
public class ExamDto : AuditedEntityDto<Guid>
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public Guid? LevelId { get; set; }
    public string? LevelName { get; set; }

    public ExamStatus Status { get; set; }
    public ExamMode Mode { get; set; }

    public int TimeLimitInMinutes { get; set; }
    public decimal PassingPercentage { get; set; }

    /// <summary>Null means every question in the bank; a number means a drawn form.</summary>
    public int? QuestionsPerForm { get; set; }

    /// <summary>How many questions the bank actually holds, so the author can see the ratio.</summary>
    public int QuestionCount { get; set; }

    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool OneQuestionAtATime { get; set; }
    public bool AllowBackNavigation { get; set; }
    public bool CollectIntegritySignals { get; set; }

    public bool IsScheduled { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
}

/// <summary>Creating or editing an exam.</summary>
public class CreateUpdateExamDto
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    public string Title { get; set; } = default!;

    [StringLength(2048)]
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }
    public Guid? LevelId { get; set; }

    public ExamMode Mode { get; set; } = ExamMode.Assessment;

    /// <summary>Anything under five minutes is almost always a mistake, not a short exam.</summary>
    [Range(1, 600)]
    public int TimeLimitInMinutes { get; set; } = 60;

    /// <summary>A percentage, not an absolute mark: forms differ in length.</summary>
    [Range(1, 100)]
    public decimal PassingPercentage { get; set; } = 60m;

    [Range(1, 500)]
    public int? QuestionsPerForm { get; set; }

    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    public bool OneQuestionAtATime { get; set; } = true;
    public bool AllowBackNavigation { get; set; } = true;
    public bool CollectIntegritySignals { get; set; } = true;

    public bool IsScheduled { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
}

/// <summary>Filters for the exam list.</summary>
public class ExamListRequestDto : PagedAndSortedResultRequestDto
{
    /// <summary>Matches the title. Kept deliberately simple; the list is browsed, not searched hard.</summary>
    public string? Filter { get; set; }

    public Guid? CategoryId { get; set; }
    public Guid? LevelId { get; set; }
    public ExamStatus? Status { get; set; }
}

/// <summary>One line of the blueprint: how many questions to draw, and from where.</summary>
public class BlueprintRuleDto : EntityDto<Guid>
{
    /// <summary>
    /// The part of the paper this rule fills, or null to fill the paper as a
    /// whole. A part that owns a rule draws from the shared bank on what the
    /// rule asks for, which is how "ten Listening and ten Reading, drawn fresh
    /// each sitting" is written down.
    /// </summary>
    public Guid? ExamSectionId { get; set; }
    public string? ExamSectionName { get; set; }

    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }

    public QuestionDifficulty? Difficulty { get; set; }
    public string? QuestionType { get; set; }

    public int QuestionCount { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// How many questions in the bank actually match this rule. Shown beside the
    /// count so an author sees "draw 8 from a pool of 5" before a candidate does.
    /// </summary>
    public int AvailableCount { get; set; }
}

public class CreateUpdateBlueprintRuleDto
{
    public Guid? ExamSectionId { get; set; }

    public Guid? TopicId { get; set; }
    public QuestionDifficulty? Difficulty { get; set; }

    [StringLength(64)]
    public string? QuestionType { get; set; }

    [Range(1, 200)]
    public int QuestionCount { get; set; } = 1;

    public int DisplayOrder { get; set; }
}

/// <summary>
/// What publishing would do, checked before it is attempted.
/// <para>
/// Publishing is the gate between a draft and something a real person will sit,
/// so the author sees every reason it would fail at once rather than fixing them
/// one refused click at a time.
/// </para>
/// </summary>
public class PublishCheckDto
{
    public bool CanPublish { get; set; }

    /// <summary>Reasons publishing is refused. Each is a localisation key.</summary>
    public List<string> Blockers { get; set; } = new();

    /// <summary>Things worth knowing that do not prevent publishing.</summary>
    public List<string> Warnings { get; set; } = new();

    public int QuestionCount { get; set; }
    public decimal TotalScore { get; set; }

    /// <summary>Questions per form once the blueprint is applied.</summary>
    public int FormLength { get; set; }
}
