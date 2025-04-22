using System;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.CandidateExamAttempts.DTOs
{
    public class SubmitCandidateExamAttemptDto
    {
        [Required]
        public Guid AttemptId { get; set; }

        [Range(0, 100)]
        public double Score { get; set; }
    }
}
