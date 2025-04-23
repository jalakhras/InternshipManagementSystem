using InternshipManagementSystem.IdentityManagement.DTOs.InternshipManagementSystem.IdentityManagement.DTOs;
using System.Threading.Tasks;

namespace InternshipManagementSystem.IdentityManagement
{
    public interface IUserRegistrationAppService
    {
        Task<UserDto> RegisterAsync(RegisterUserDto input);
    }
}