using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.Assessment.Delivery.Dtos;

/// <summary>
/// Hands an exam to one person or a whole cohort.
/// <para>
/// The group form is the primary case, not a convenience. A language school with
/// forty students per level will not create forty links by hand; it will go back to
/// a spreadsheet, and the product loses the customer.
/// </para>
/// </summary>
public class CreateAssignmentDto
{
    [Required]
    public Guid ExamId { get; set; }

    /// <summary>Set this or <see cref="CandidateGroupId"/>, not both.</summary>
    public Guid? CandidateId { get; set; }

    public Guid? CandidateGroupId { get; set; }

    /// <summary>When the links stop working.</summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 1;

    /// <summary>Email each link on creation.</summary>
    public bool SendEmail { get; set; } = true;

    [MaxLength(1024)]
    public string? Note { get; set; }
}

/// <summary>What an assignment produced, including who could not be reached.</summary>
public class AssignmentResultDto
{
    public Guid AssignmentId { get; set; }

    public int LinksCreated { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsFailed { get; set; }

    /// <summary>
    /// Named so the operator can act. A silent count of failures is not actionable —
    /// they need to know which student did not get their link.
    /// </summary>
    public List<AssignmentRecipientDto> Recipients { get; set; } = new();
}

public class AssignmentRecipientDto
{
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = default!;
    public string Email { get; set; } = default!;

    /// <summary>
    /// The full link. Returned once, at creation, so the operator can copy it if the
    /// email bounced. It is not retrievable later: only the hash is stored.
    /// </summary>
    public string Url { get; set; } = default!;

    public bool EmailSent { get; set; }
    public string? EmailError { get; set; }
}

/// <summary>A link as staff see it. The token itself is never included.</summary>
public class ExamLinkDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = default!;

    /// <summary>First characters only, so support can identify a link without holding it.</summary>
    public string TokenPrefix { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? FirstOpenedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
}
