using Microsoft.AspNetCore.Http;
using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers
{
    public class CreateUpdateCandidateExamAnswerDto
    {
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }
        public IFormFile? AnswerFile { get; set; }
        public bool? IsCorrect { get; set; }
        public double? PartialScore { get; set; }
        public string? ReviewComments { get; set; }

    }
}