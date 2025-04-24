using System;

namespace InternshipManagementSystem.TrainingManagement.ExamLinks.DTOs
{
    public class ExamAccessResultDto
    {
        public bool IsAccessible { get; set; }
        public string Message { get; set; }
        public Guid? CandidateId { get; set; }
        public Guid? ExamId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int MaxAttempts { get; set; }
        public int CurrentAttempts { get; set; }
    }
    public class StartExamFromLinkResultDto
    {
        public bool IsStarted { get; set; }
        public string Message { get; set; }
        public Guid? CandidateId { get; set; }
        public Guid? ExamId { get; set; }
    }

}