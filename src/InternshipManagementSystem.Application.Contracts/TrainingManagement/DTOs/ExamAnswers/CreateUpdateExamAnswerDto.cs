using Microsoft.AspNetCore.Http;
using System;

namespace InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers
{
    public class CreateUpdateExamAnswerDto
    {
        public Guid QuestionId { get; set; } // معرّف السؤال الذي جاوب عليه
        public string Answer { get; set; }    // نص الإجابة التي كتبها المتدرب
        public IFormFile? AnswerFile { get; set; } // ملف مرفق للإجابة (اختياري)
    }
}