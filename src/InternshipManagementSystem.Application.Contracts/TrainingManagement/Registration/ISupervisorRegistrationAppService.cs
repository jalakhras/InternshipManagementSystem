using InternshipManagementSystem.IdentityManagement;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.TrainingManagement.Registration
{
    public interface ISupervisorRegistrationAppService : IApplicationService
    {
        Task<UserDto> RegisterAsync(CreateSupervisorUserDto input);
    }
}