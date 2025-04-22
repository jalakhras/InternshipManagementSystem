using InternshipManagementSystem.IdentityManagement.DTOs;
using InternshipManagementSystem.IdentityManagement.DTOs.InternshipManagementSystem.IdentityManagement.DTOs;
using System.Threading.Tasks;
using Volo.Abp.Identity;

namespace InternshipManagementSystem.IdentityManagement
{
    public interface IUserRegistrationAppService
    {
        Task<UserDto> RegisterAsync(RegisterUserDto input);
    }
}