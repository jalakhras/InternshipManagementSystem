using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;

public class Question : AuditedAggregateRoot<Guid>
{
    public Guid ExamId { get; set; }
    public string Text { get; set; }
    public QuestionType Type { get; set; }
    public string OptionsJson { get; set; } // لو فيه اختيارات
    public string CorrectAnswer { get; set; }

    public double Score { get; set; } // عدد النقاط للسؤال
    public int? TimeLimitInSeconds { get; set; } // وقت محدد للسؤال (اختياري)
    public string? MediaUrl { get; set; } // رابط ملف وسائط مرفق بالسؤال (صورة، صوت، فيديو)
    public MediaType? MediaType { get; set; } // نوع الملف (صورة - صوت - فيديو - مستند)
    public string? MediaFileName { get; set; } // اسم الملف الفعلي (اختياري)

    public bool AllowPartialCredit { get; set; } // لو MultiSelect، هل نسمح بدرجات جزئية؟

    public string? CodeStarterTemplate { get; set; } // الكود الابتدائي الذي يظهر للطالب
    public string? CodeExpectedOutput { get; set; }  // المخرجات المتوقعة (للتقييم الآلي)
    public string? CodeLanguage { get; set; }        // لغة البرمجة (مثل C#, JS, Python)

    public Exam Exam { get; set; }
}