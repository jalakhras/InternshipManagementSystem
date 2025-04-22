using System;
using System.Collections.Generic;

using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class Specialization : AuditedAggregateRoot<Guid>
    {
        public string Name { get; set; }

        public ICollection<Exam> Exams { get; set; }
        public ICollection<Trainee> Trainees { get; set; }
    }
}