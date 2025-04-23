using InternshipManagementSystem.TrainingManagement;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class CandidateExamAttempt : AuditedAggregateRoot<Guid>
    {
        public Guid CandidateId { get; set; }
        public Guid ExamId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Score { get; set; }
        public bool IsPassed { get; set; }

        public Candidate Candidate { get; set; }
        public Exam Exam { get; set; }
        public bool IsSubmitted { get; set; }
        public ICollection<CandidateExamAnswer> CandidateExamAnswers { get; set; } = new List<CandidateExamAnswer>();

        public CandidateExamAttempt()
        {
            
        }

        public CandidateExamAttempt(Guid id) : base(id)
        {
        }
    }


}
