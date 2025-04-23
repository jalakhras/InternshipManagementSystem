using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Exams
{
    public class CreateUpdateExamDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid SpecializationId { get; set; }
        public string Level { get; set; }
        public int TimeLimitInMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsActive { get; set; }
    }
}