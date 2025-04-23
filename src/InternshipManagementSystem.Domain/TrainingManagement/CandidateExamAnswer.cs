using InternshipManagementSystem.TrainingManagement;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement;
public class CandidateExamAnswer : AuditedAggregateRoot<Guid>
    {
        public Guid CandidateExamAttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }

        public string? AnswerFileUrl { get; set; }
        public string? AnswerFileName { get; set; }

        // Navigation properties
        public CandidateExamAttempt CandidateExamAttempt { get; set; }
        public Question Question { get; set; }
    
}
