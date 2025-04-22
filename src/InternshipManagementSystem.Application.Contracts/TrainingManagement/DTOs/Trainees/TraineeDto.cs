using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Trainees
{
    public class TraineeDto : AuditedEntityDto<Guid>
    {
        public string FullName { get; set; }
        public string EmployeeNumber { get; set; }
        public Guid SpecializationId { get; set; }
        public string SpecializationName { get; set; }
        public Guid? UserId { get; set; } 

    }
}