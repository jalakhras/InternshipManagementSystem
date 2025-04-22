using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Candidates.DTOs
{
    public class CandidateDto : AuditedEntityDto<Guid>
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Specialization { get; set; }
        public string Notes { get; set; }
    }
    }

