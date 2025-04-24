using System;

namespace InternshipManagementSystem.CandidateExamAnswers.DTOs
{
    public class ReviewAnswerInput
    {
        public Guid AnswerId { get; set; }
        public double Score { get; set; }
        public string? Comments { get; set; }
    }

}