using System;
using System.Collections.Generic;

using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class ExamAttempt : AuditedAggregateRoot<Guid>
    {
        public Guid ExamId { get; set; }
        public Guid TraineeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Score { get; set; }
        public bool IsPassed { get; set; }
        public bool IsGraded { get; set; } // هل تم تصحيح المحاولة
        public bool NeedsManualReview { get; set; } // هل هناك أسئلة تحتاج تصحيح يدوي
        public Exam Exam { get; set; }
        public Trainee Trainee { get; set; }
        public ICollection<ExamAnswer> ExamAnswers { get; set; }
    }
}