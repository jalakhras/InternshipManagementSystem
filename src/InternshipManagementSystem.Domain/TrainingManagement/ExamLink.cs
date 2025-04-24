using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace InternshipManagementSystem.TrainingManagement
{
    public class ExamLink : AuditedAggregateRoot<Guid>
    {
        public ExamLink(Guid id) : base(id)
        {
        }
        public ExamLink()
        {
                
        }

        public Guid ExamId { get; set; }
        public Guid CandidateId { get; set; }
        public string SecureToken { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int MaxAttempts { get; set; }
        public int CurrentAttempts { get; set; }


        public Candidate Candidate { get; set; }
        public Exam Exam { get; set; }
    }
}
