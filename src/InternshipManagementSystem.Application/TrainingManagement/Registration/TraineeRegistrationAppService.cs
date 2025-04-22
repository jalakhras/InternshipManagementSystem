using InternshipManagementSystem.Identity;
using InternshipManagementSystem.IdentityManagement;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace InternshipManagementSystem.TrainingManagement.Registration
{
    [AllowAnonymous]
    public class TraineeRegistrationAppService : ApplicationService, ITraineeRegistrationAppService
    {
        private readonly IIdentityUserAppService _userAppService;
        private readonly IRepository<Trainee, Guid> _traineeRepository;
        private readonly IIdentityRoleRepository _roleRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IObjectMapper _objectMapper;

        public TraineeRegistrationAppService(
            IIdentityUserAppService userAppService,
            IRepository<Trainee, Guid> traineeRepository,
            IIdentityRoleRepository roleRepository,
            IGuidGenerator guidGenerator, IObjectMapper objectMapper)
        {
            _userAppService = userAppService;
            _traineeRepository = traineeRepository;
            _roleRepository = roleRepository;
            _guidGenerator = guidGenerator;
            _objectMapper = objectMapper;
        }

        public async Task<UserDto> RegisterTraineeAsync(CreateTraineeUserDto input)
        {
            var user = await _userAppService.CreateAsync(new IdentityUserCreateDto
            {
                UserName = input.UserName,
                Email = input.Email,
                Password = input.Password,
                PhoneNumber = input.PhoneNumber,
                RoleNames = new[] { RoleNames.Trainee }
            });

            var trainee = new Trainee
            {
                FullName = input.UserName,
                EmployeeNumber = Guid.NewGuid().ToString().Substring(0, 8), // توليد رقم عشوائي صغير
                SpecializationId = input.SpecializationId,
                UserId = user.Id
            };

            await _traineeRepository.InsertAsync(trainee);

            return _objectMapper.Map<IdentityUserDto, UserDto>(user);
        }
    }
}