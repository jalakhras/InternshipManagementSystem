using InternshipManagementSystem.Identity;
using InternshipManagementSystem.IdentityManagement;
using InternshipManagementSystem.IdentityManagement.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace InternshipManagementSystem.TrainingManagement.Registration
{
    [AllowAnonymous]
    public class SupervisorRegistrationAppService : InternshipManagementSystemAppService, ISupervisorRegistrationAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IObjectMapper _objectMapper;

        public SupervisorRegistrationAppService(
            IdentityUserManager userManager,
            IObjectMapper objectMapper)
        {
            _userManager = userManager;
            _objectMapper = objectMapper;
        }

        public async Task<UserDto> RegisterAsync(CreateSupervisorUserDto input)
        {
            var user = new IdentityUser(
                GuidGenerator.Create(),
                input.UserName,
                input.Email,
                CurrentTenant.Id
            );

            (await _userManager.CreateAsync(user, input.Password)).CheckErrors();

            return _objectMapper.Map<IdentityUser, UserDto>(user);
        }
    }
}