using InternshipManagementSystem.TrainingManagement.Grading.DTOs;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.TrainingManagement.Grading
{
    public interface IReviewDashboardAppService
    {
        Task<ListResultDto<ManualReviewAttemptDto>> GetAttemptsNeedingManualReviewAsync();
    }
}