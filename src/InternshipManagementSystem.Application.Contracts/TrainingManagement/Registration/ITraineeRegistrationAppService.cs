using InternshipManagementSystem.IdentityManagement;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.Registration
{
    public interface ITraineeRegistrationAppService : IApplicationService
    {
        Task<UserDto> RegisterTraineeAsync(CreateTraineeUserDto input);
    }
}