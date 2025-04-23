using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Specializations
{
    public class SpecializationDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
    }
}