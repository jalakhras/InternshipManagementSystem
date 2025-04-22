using System;

using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class ExamAnswer : AuditedAggregateRoot<Guid>
    {
        public Guid ExamAttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }
        public bool? IsCorrect { get; set; } // صحيح / خطأ (nullable لأنه مش كل الأسئلة يمكن تقييمها تلقائياً)
        public double? PartialScore { get; set; } // لو السؤال يعطي جزء من الدرجة
        public string ReviewComments { get; set; } // ملاحظات عند المراجعة اليدوية
        public ExamAttempt ExamAttempt { get; set; }
        public Question Question { get; set; }
    }
}