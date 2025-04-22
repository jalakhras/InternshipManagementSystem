using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Questions
{
    public class QuestionDto : AuditedEntityDto<Guid>
    {
        public Guid ExamId { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public string OptionsJson { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
