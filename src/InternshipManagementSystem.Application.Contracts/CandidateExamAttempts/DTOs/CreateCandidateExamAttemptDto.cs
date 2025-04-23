using System;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.CandidateExamAttempts.DTOs
{
    public class CreateCandidateExamAttemptDto
    {
        [Required]
        public Guid CandidateId { get; set; }

        [Required]
        public Guid ExamId { get; set; }
    }
}