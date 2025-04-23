using System;

namespace InternshipManagementSystem.CandidateExamAttempts.DTOs;

public class CandidateExamAttemptResultDto
{
    public Guid AttemptId { get; set; }
    public double Score { get; set; }
    public bool Passed { get; set; }
}