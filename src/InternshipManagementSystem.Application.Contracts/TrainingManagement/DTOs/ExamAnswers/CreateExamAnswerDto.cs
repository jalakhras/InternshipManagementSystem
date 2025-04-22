using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers
{
    public class CreateExamAnswerDto
    {
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }
    }
}
