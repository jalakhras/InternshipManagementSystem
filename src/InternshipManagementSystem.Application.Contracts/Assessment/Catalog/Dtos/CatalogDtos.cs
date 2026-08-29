using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.Assessment.Catalog.Dtos;

/// <summary>
/// A domain the organisation tests in: English, safety, trading, retail sales.
/// <para>
/// The word "category" is ours, not the customer's. A language centre calls these
/// languages, a factory calls them competencies, a broker calls them desks — which
/// is what <see cref="CategorySetDto"/> is for.
/// </para>
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>Short, stable, and the thing a spreadsheet import matches on.</summary>
    public string Code { get; set; } = default!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Levels under this domain, in order. Beginner before advanced.</summary>
    public List<LevelDto> Levels { get; set; } = new();

    /// <summary>
    /// Topics under this domain, flat, each carrying its parent.
    /// <para>
    /// Flat rather than nested because the tree is two or three deep in practice
    /// and every consumer — the blueprint editor, the topic breakdown on a result —
    /// wants to look one up by id rather than walk to it.
    /// </para>
    /// </summary>
    public List<TopicDto> Topics { get; set; } = new();

    /// <summary>
    /// How many exams and questions point at this, so a coordinator can see what
    /// deactivating it would affect before doing it.
    /// </summary>
    public int ExamCount { get; set; }

    public int QuestionCount { get; set; }
}

/// <summary>One rung: A1, B2, beginner, level three.</summary>
public class LevelDto
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }

    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;

    /// <summary>The ladder's order. Two levels sharing a number sort by name.</summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// What a question is about, which is what makes a result say more than a number.
/// </summary>
public class TopicDto
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }

    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;

    /// <summary>Null at the top. "Grammar" holds "tenses" holds "past perfect".</summary>
    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

public class CreateUpdateCategoryDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [StringLength(512)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CreateUpdateLevelDto
{
    /// <summary>
    /// The domain this rung belongs to. Null means it applies everywhere, which is
    /// right for an organisation whose levels are the same across subjects.
    /// </summary>
    public Guid? CategoryId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CreateUpdateTopicDto
{
    public Guid? CategoryId { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// The tenant's own words for all of this.
/// <para>
/// A recruiter should not be reading school vocabulary. Renaming "level" to "grade
/// band" or "candidate" to "trainee" is not decoration: staff who see their own
/// words trust what the screen is telling them, and staff who see somebody else's
/// spend the first week translating.
/// </para>
/// </summary>
public class CategorySetDto
{
    /// <summary>What one domain is called. "Language", "competency", "desk".</summary>
    public string SingularName { get; set; } = default!;

    public string PluralName { get; set; } = default!;

    /// <summary>What the person sitting the exam is called.</summary>
    public string SubjectSingularName { get; set; } = default!;

    public string SubjectPluralName { get; set; } = default!;

    /// <summary>What a set of them is called. "Class", "cohort", "intake", "shift".</summary>
    public string GroupSingularName { get; set; } = default!;

    public string GroupPluralName { get; set; } = default!;
}

public class UpdateCategorySetDto
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string SingularName { get; set; } = default!;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string PluralName { get; set; } = default!;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string SubjectSingularName { get; set; } = default!;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string SubjectPluralName { get; set; } = default!;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string GroupSingularName { get; set; } = default!;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string GroupPluralName { get; set; } = default!;
}
