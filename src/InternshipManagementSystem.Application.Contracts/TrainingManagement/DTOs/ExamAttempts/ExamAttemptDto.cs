using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts
{
    public class ExamAttemptDto : AuditedEntityDto<Guid>
    {
        public Guid ExamId { get; set; }
        public Guid TraineeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Score { get; set; }
        public bool IsPassed { get; set; }
    }
}