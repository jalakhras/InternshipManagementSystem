using System;

using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class ExamAnswer : AuditedAggregateRoot<Guid>
    {
        public Guid ExamAttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }

        public ExamAttempt ExamAttempt { get; set; }
        public Question Question { get; set; }
    }
}