using System;

namespace InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs
{
    public class ExamLinkDto
    {
        public Guid ExamId { get; set; }
        public Guid CandidateId { get; set; }
        public string SecureLink { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int MaxAttempts { get; set; }
        public int CurrentAttempts { get; set; }
    }
}