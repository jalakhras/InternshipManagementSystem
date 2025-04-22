using System.Collections.Generic;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class Candidate : AuditedAggregateRoot<Guid>
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PositionAppliedFor { get; set; } // الوظيفة المتقدم لها
        public CandidateStatus Status { get; set; }

        public ICollection<CandidateExamAttempt> ExamAttempts { get; set; }
    }

    public enum CandidateStatus
    {
        Pending,  // قيد التقييم
        Passed,   // ناجح
        Failed    // راسب
    }
}
