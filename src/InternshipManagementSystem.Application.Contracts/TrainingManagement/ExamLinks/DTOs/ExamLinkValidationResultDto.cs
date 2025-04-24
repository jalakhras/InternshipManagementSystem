using System;

namespace InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs
{
    public class ExamLinkValidationResultDto
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public Guid? ExamId { get; set; }
    }
}