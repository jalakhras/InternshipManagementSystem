using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.ExamAnswers
{
    public class ExamAnswerDto : AuditedEntityDto<Guid>
    {
        public Guid ExamAttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }
    }
}
