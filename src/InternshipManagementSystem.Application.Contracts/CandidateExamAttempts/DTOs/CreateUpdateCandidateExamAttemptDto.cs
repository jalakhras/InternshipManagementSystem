using System;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.TrainingManagement.CandidateExamAttempts.DTOs;

public class CreateUpdateCandidateExamAttemptDto
{
    [Required]
    public Guid CandidateId { get; set; }

    [Required]
    public Guid ExamId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public double Score { get; set; }
    public bool IsPassed { get; set; }
}