using System;
using System.Threading.Tasks;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public interface IExamGradingService
    {
        Task EvaluateExamAttemptAsync(Guid attemptId);
    }
}
