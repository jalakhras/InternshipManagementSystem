using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.Candidates.DTOs;

public class CandidateDto : AuditedEntityDto<Guid>
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string AppliedPosition { get; set; }
    public string Notes { get; set; }
}