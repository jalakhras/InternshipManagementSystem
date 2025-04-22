using InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers;
using System;
using System.Collections.Generic;

namespace InternshipManagementSystem.TrainingManagement.DTOs.ExamAttempts
{
    public class CreateExamAttemptDto
    {
        public Guid ExamId { get; set; }
        public Guid TraineeId { get; set; }
        public List<CreateExamAnswerDto> Answers { get; set; }
    }
}
