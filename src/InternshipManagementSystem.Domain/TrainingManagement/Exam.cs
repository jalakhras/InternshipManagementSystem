using System;
using System.Collections.Generic;

using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class Exam : AuditedAggregateRoot<Guid>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid SpecializationId { get; set; }
        public string Level { get; set; } // Beginner / Intermediate / Advanced
        public int TimeLimitInMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsActive { get; set; }

        public Specialization Specialization { get; set; }
        public ICollection<Question> Questions { get; set; }
        public ICollection<ExamAttempt> ExamAttempts { get; set; }
    }
}