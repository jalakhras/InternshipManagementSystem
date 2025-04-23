using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.TrainingManagement.Candidates.DTOs;

public class CreateUpdateCandidateDto
{
    [Required]
    [StringLength(256)]
    public string FullName { get; set; }

    [Required]
    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    [Required]
    public string AppliedPosition { get; set; } // الوظيفة التي يقدم لها

    public string Notes { get; set; } // ملاحظات إضافية
}