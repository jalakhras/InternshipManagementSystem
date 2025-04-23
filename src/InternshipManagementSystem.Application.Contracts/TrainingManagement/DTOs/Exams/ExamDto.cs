using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Exams
{
    public class ExamDto : AuditedEntityDto<Guid>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid SpecializationId { get; set; }
        public string Level { get; set; }
        public int TimeLimitInMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsActive { get; set; }
        public string SpecializationName { get; set; }
    }
}