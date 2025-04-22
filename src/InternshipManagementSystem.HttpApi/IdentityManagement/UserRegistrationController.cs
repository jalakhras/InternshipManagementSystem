using InternshipManagementSystem.IdentityManagement.DTOs;
using InternshipManagementSystem.IdentityManagement.DTOs.InternshipManagementSystem.IdentityManagement.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.IdentityManagement
{
    [Route("api/identity/register")]
    public class UserRegistrationController : AbpControllerBase
    {
        private readonly IUserRegistrationAppService _userRegistrationAppService;

        public UserRegistrationController(IUserRegistrationAppService userRegistrationAppService)
        {
            _userRegistrationAppService = userRegistrationAppService;
        }

        [HttpPost]
        public async Task<UserDto> RegisterAsync(RegisterUserDto input)
        {
            return await _userRegistrationAppService.RegisterAsync(input);
        }
    }
}
