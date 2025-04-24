using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternshipManagementSystem.CandidateExamAnswers.DTOs
{
    public class CandidateExamAnswerForReviewDto
    {
        public Guid Id { get; set; }
        public string QuestionText { get; set; }
        public string Answer { get; set; }
        public string? AnswerFileUrl { get; set; }
        public string? AnswerFileName { get; set; }
        public double? PartialScore { get; set; }
        public string? ReviewComments { get; set; }
    }
}
