using InternshipManagementSystem.TrainingManagement.Registration;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.IdentityManagement
{
    [Route("api/registration/trainee")]
    public class TraineeRegistrationController : AbpController
    {
        private readonly ITraineeRegistrationAppService _traineeRegistrationAppService;
        private readonly ISupervisorRegistrationAppService _supervisorRegistrationAppService;

        public TraineeRegistrationController(ITraineeRegistrationAppService traineeRegistrationAppService, ISupervisorRegistrationAppService supervisorRegistrationAppService)
        {
            _traineeRegistrationAppService = traineeRegistrationAppService;
            _supervisorRegistrationAppService = supervisorRegistrationAppService;
        }

        [HttpPost]
        public async Task<UserDto> RegisterTraineeAsync(CreateTraineeUserDto input)
        {
            return await _traineeRegistrationAppService.RegisterTraineeAsync(input);
        }

    } 
    
    
    [Route("api/registration/supervisor")]
    public class SupervisorRegistrationController : AbpController
    {
        private readonly ISupervisorRegistrationAppService _supervisorRegistrationAppService;

        public SupervisorRegistrationController(ISupervisorRegistrationAppService supervisorRegistrationAppService)
        {
            _supervisorRegistrationAppService = supervisorRegistrationAppService;
        }

        [HttpPost]
        public async Task<UserDto> RegisterTraineeAsync(CreateSupervisorUserDto input)
        {
            return await _supervisorRegistrationAppService.RegisterAsync(input);
        }
    }

}
