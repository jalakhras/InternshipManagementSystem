using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Settings;

namespace InternshipManagementSystem.Settings
{
    public class SelfRegistrationSettingAppService : ApplicationService , ISelfRegistrationSettingAppService
    {
        private readonly ISettingProvider _settingProvider;

        public SelfRegistrationSettingAppService(ISettingProvider settingProvider)
        {
            _settingProvider = settingProvider;
        }

        public async Task<bool> IsSelfRegistrationEnabledAsync()
        {
            var setting = await _settingProvider.GetAsync<bool>(InternshipManagementSystemSettings.EnableSelfRegistration);
            return setting;
        }
    }
}