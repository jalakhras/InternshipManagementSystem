using InternshipManagementSystem.TrainingManagement.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Questions
{
    public class QuestionDto : AuditedEntityDto<Guid>
    {
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public string OptionsJson { get; set; }
        public string CorrectAnswer { get; set; }
        public string AttachmentUrl { get; set; }
        public int Score { get; set; }
        public int? TimeLimitInSeconds { get; set; }
        public Guid ExamId { get; set; }
        public string? MediaUrl { get; set; }

        public MediaType? MediaType { get; set; }
    }
}
