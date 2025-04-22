using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Questions
{
    public class CreateUpdateQuestionDto
    {
        public Guid ExamId { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public string OptionsJson { get; set; }
        public string CorrectAnswer { get; set; }
        public int Score { get; set; }
        public int? TimeLimitInSeconds { get; set; }
        public string MediaPath { get; set; }
    }
}
