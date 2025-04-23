using InternshipManagementSystem.Identity;
using InternshipManagementSystem.IdentityManagement.DTOs.InternshipManagementSystem.IdentityManagement.DTOs;
using InternshipManagementSystem.Settings;
using InternshipManagementSystem.TrainingManagement;
using InternshipManagementSystem.TrainingManagement.IRepositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Settings;

namespace InternshipManagementSystem.IdentityManagement
{
    public class UserRegistrationAppService : ApplicationService, IUserRegistrationAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _roleManager;
        private readonly ITraineeRepository _traineeRepository;
        private readonly IObjectMapper _objectMapper;
        private readonly ISettingProvider _settingProvider;

        public UserRegistrationAppService(
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            ITraineeRepository traineeRepository,
            IObjectMapper objectMapper, ISettingProvider settingProvider)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _traineeRepository = traineeRepository;
            _objectMapper = objectMapper;
            _settingProvider = settingProvider;
        }

        public async Task<UserDto> RegisterAsync(RegisterUserDto input)
        {
            if (CurrentUser?.IsAuthenticated != true)
            {
                var isSelfRegistrationEnabled = await _settingProvider.GetAsync<bool>(InternshipManagementSystemSettings.EnableSelfRegistration);
                if (!isSelfRegistrationEnabled)
                {
                    throw new BusinessException("Self-registration is currently disabled.");
                }
            }
            var existingUser = await _userManager.FindByEmailAsync(input.Email);
            if (existingUser != null)
            {
                throw new UserFriendlyException("Email is already taken.");
            }

            var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.Email, CurrentTenant.Id)
            {
                Name = input.FullName
            };

            (await _userManager.CreateAsync(user, input.Password)).CheckErrors();

            string roleName = input.UserType switch
            {
                UserType.Trainee => RoleNames.Trainee,
                UserType.Supervisor => RoleNames.Supervisor,
                _ => throw new UserFriendlyException("Invalid user type.")
            };

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                throw new UserFriendlyException($"Role '{roleName}' not found. Please check DataSeeder.");
            }

            await _userManager.AddToRoleAsync(user, roleName);

            if (input.UserType == UserType.Trainee)
            {
                // انشاء متدرب وربطه بالمستخدم
                var trainee = new Trainee
                {
                    FullName = input.FullName,
                    EmployeeNumber = $"EMP-{Guid.NewGuid().ToString("N").Substring(0, 8)}", // رقم وظيفي مؤقت
                    UserId = user.Id,
                    SpecializationId = Guid.Empty // سيتم تعيينه لاحقًا
                };

                await _traineeRepository.InsertAsync(trainee, autoSave: true);
            }

            return _objectMapper.Map<IdentityUser, UserDto>(user);
        }
    }
}