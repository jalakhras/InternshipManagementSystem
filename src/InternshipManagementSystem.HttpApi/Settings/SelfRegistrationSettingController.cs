using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace InternshipManagementSystem.Settings
{
    [Route("api/settings/self-registration")]
    public class SelfRegistrationSettingController : AbpControllerBase
    {
        private readonly ISelfRegistrationSettingAppService _selfRegistrationSettingAppService;

        public SelfRegistrationSettingController(ISelfRegistrationSettingAppService selfRegistrationSettingAppService)
        {
            _selfRegistrationSettingAppService = selfRegistrationSettingAppService;
        }

        [HttpGet]
        [AllowAnonymous] // متاح للجميع بدون تسجيل دخول
        public async Task<bool> GetIsSelfRegistrationEnabledAsync()
        {
            return await _selfRegistrationSettingAppService.IsSelfRegistrationEnabledAsync();
        }
    }
}
