using InternshipManagementSystem.TrainingManagement.DTOs.CandidateExamAnswers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public interface ICandidateExamReviewAppService
    {
        Task<List<CandidateExamAnswerDto>> GetAnswersForManualReviewAsync(Guid attemptId);

        Task ReviewAnswerAsync(Guid answerId, int partialScore, string? comment);
    }
}