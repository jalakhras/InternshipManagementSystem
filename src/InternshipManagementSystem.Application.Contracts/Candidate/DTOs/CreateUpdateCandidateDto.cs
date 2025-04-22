using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.Candidates.DTOs
{
    public class CreateUpdateCandidateDto
        {
            [Required]
            [StringLength(128)]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; set; }

            [Phone]
            [StringLength(20)]
            public string Phone { get; set; }

            [Required]
            [StringLength(128)]
            public string Specialization { get; set; }

            [StringLength(1024)]
            public string Notes { get; set; }
        }
    }

