using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers
{
    public class CandidateExamAnswerDto : AuditedEntityDto<Guid>
    {
        public Guid CandidateExamAttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string Answer { get; set; }

        public string? AnswerFileUrl { get; set; }
        public string? AnswerFileName { get; set; }
        public bool? IsCorrect { get; set; }
        public double? PartialScore { get; set; }
        public string? ReviewComments { get; set; }

    }
}