using System;

namespace InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs
{
    public class CreateExamLinkInput
    {
        public Guid ExamId { get; set; }
        public Guid CandidateId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int MaxAttempts { get; set; } = 1;
    }
}