using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.CandidateExamAttempts.DTOs;

public class CandidateExamAttemptDto : AuditedEntityDto<Guid>
{
    public Guid CandidateId { get; set; }
    public Guid ExamId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? Score { get; set; }
    public bool? IsPassed { get; set; }
    public Guid Id { get; set; }
    public bool IsSubmitted { get; set; }
}

