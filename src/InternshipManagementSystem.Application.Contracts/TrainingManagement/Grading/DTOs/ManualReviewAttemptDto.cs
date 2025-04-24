using System;

namespace InternshipManagementSystem.TrainingManagement.Grading.DTOs
{
    public class ManualReviewAttemptDto
    {
        public Guid AttemptId { get; set; }
        public string CandidateName { get; set; }
        public string ExamTitle { get; set; }
        public DateTime StartTime { get; set; }
        public double Score { get; set; }
        public bool IsSubmitted { get; set; }
    }
}