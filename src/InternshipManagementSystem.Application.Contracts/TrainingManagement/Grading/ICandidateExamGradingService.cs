using System;
using System.Threading.Tasks;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public interface ICandidateExamGradingService
    {
        Task EvaluateCandidateExamAttemptAsync(Guid attemptId);
    }
}