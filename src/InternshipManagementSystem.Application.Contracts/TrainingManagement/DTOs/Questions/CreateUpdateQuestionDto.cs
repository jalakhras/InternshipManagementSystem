using InternshipManagementSystem.TrainingManagement.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.TrainingManagement.DTOs.Questions
{
    public class CreateUpdateQuestionDto
    {
        [Required]
        [StringLength(1024)]
        public string Text { get; set; }

        [Required]
        public QuestionType Type { get; set; }

        public string OptionsJson { get; set; } // اختيارات لو Multiple Choice

        public string CorrectAnswer { get; set; } // النص الصحيح أو الكود البرمجي الصحيح

        public string? AttachmentUrl { get; set; } // رابط الملف المرفق (صوت، فيديو، صورة...الخ)

        public int Score { get; set; } // كم علامة على هذا السؤال

        public int? TimeLimitInSeconds { get; set; } // وقت محدد لحل السؤال (اختياري)
        public IFormFile? MediaFile { get; set; }
        public string? MediaUrl { get; set; } // الرابط للوسائط المرفقة
        public MediaType? MediaType { get; set; }
        public Guid ExamId { get; set; }
        public string? CodeStarterTemplate { get; set; }
        public string? CodeExpectedOutput { get; set; }
        public string? CodeLanguage { get; set; }

    }
}