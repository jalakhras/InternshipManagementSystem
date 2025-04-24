using System;

namespace InternshipManagementSystem.TrainingManagement.ExamResults.DTOs
{
    public class ExamResultSummaryDto
    {
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; }
        public int TotalAttempts { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
    }
}