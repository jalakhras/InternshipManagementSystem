using InternshipManagementSystem.TrainingManagement.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;


namespace InternshipManagementSystem.TrainingManagement
{
    public class Question : AuditedAggregateRoot<Guid>
    {
        public Guid ExamId { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public string OptionsJson { get; set; } // لو السؤال اختيار من متعدد
        public string CorrectAnswer { get; set; }

        public Exam Exam { get; set; }
    }
}