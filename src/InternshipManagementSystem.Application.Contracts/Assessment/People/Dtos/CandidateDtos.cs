using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using InternshipManagementSystem.Assessment;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Assessment.People.Dtos;

public class CandidateDto : AuditedEntityDto<Guid>
{
    public string FullName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string? PhoneNumber { get; set; }

    public Guid? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    /// <summary>The tenant's own identifier for this person — a student number, an applicant reference.</summary>
    public string? Reference { get; set; }

    public CandidateStatus Status { get; set; }

    /// <summary>Cohorts this person belongs to, so a list row explains why they are here.</summary>
    public List<string> GroupNames { get; set; } = new();

    public int AttemptCount { get; set; }
}

public class CreateUpdateCandidateDto
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string FullName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(64)]
    public string? Reference { get; set; }
}

public class CandidateListRequestDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? GroupId { get; set; }

    public CandidateStatus? Status { get; set; }
}

// ----------------------------------------------------------------- cohorts

public class CandidateGroupDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public int MemberCount { get; set; }
}

public class CreateUpdateCandidateGroupDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [StringLength(512)]
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }
}

public class SetGroupMembersDto
{
    [Required]
    public List<Guid> CandidateIds { get; set; } = new();
}

// ------------------------------------------------------------------ import

/// <summary>
/// A pasted list of people.
/// <para>
/// The single largest thing standing between a training centre and using this
/// product is that their students are already in a spreadsheet. Retyping forty
/// names is the reason a trial stops on the first evening.
/// </para>
/// </summary>
public class ImportCandidatesDto
{
    /// <summary>
    /// One person per line: name, then email, then optionally a phone number and a
    /// reference. Separated by a comma or a tab, so a paste straight out of a
    /// spreadsheet works without anyone converting anything.
    /// </summary>
    [Required]
    [StringLength(200_000, MinimumLength = 1)]
    public string Text { get; set; } = default!;

    public Guid? CategoryId { get; set; }

    /// <summary>Adds everyone imported to this cohort, which is usually the point.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Checks the list and reports what would happen without writing anything.
    /// <para>
    /// Somebody pasting forty rows should see the three that are wrong before
    /// committing, not afterwards.
    /// </para>
    /// </summary>
    public bool DryRun { get; set; }
}

public class ImportCandidatesResultDto
{
    public int Created { get; set; }

    /// <summary>Matched by email and left alone. Importing twice must not double the roll.</summary>
    public int AlreadyPresent { get; set; }

    public int AddedToGroup { get; set; }

    public List<ImportProblemDto> Problems { get; set; } = new();
}

public class ImportProblemDto
{
    /// <summary>One-based, and counted over the pasted text so it matches what the user is looking at.</summary>
    public int Line { get; set; }

    public string Content { get; set; } = default!;

    /// <summary>A localisation key, so the reason reads in the reader's language.</summary>
    public string Reason { get; set; } = default!;
}
