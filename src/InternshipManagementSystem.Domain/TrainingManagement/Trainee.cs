using System;
using System.Collections.Generic;

using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class Trainee : AuditedAggregateRoot<Guid>
    {
        public string FullName { get; set; }
        public string EmployeeNumber { get; set; } // الرقم الوظيفي
        public Guid SpecializationId { get; set; }
        public Guid? UserId { get; set; }
        public Specialization Specialization { get; set; }
        public ICollection<ExamAttempt> ExamAttempts { get; set; }
    }
}