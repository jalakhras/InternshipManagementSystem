using InternshipManagementSystem.Settings.DTOs;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;

namespace InternshipManagementSystem.SystemSettings
{
    public class SystemGeneralSettingsAppService : ApplicationService, ISystemGeneralSettingsAppService
    {
        private readonly ISettingManager _settingManager;

        public SystemGeneralSettingsAppService(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        public async Task<GeneralSettingsDto> GetAsync()
        {
            return new GeneralSettingsDto
            {
                SiteName = await SettingProvider.GetOrNullAsync("App.General.SiteName"),
                DefaultLanguage = await SettingProvider.GetOrNullAsync("App.General.DefaultLanguage"),
                MaxExamAttempts = int.Parse(await SettingProvider.GetOrNullAsync("App.General.MaxExamAttempts") ?? "1"),
                LogoUrl = await SettingProvider.GetOrNullAsync("App.General.LogoUrl")
            };
        }

        public async Task UpdateAsync(UpdateGeneralSettingsDto input)
        {
            await _settingManager.SetForCurrentTenantAsync("App.General.SiteName", input.SiteName);
            await _settingManager.SetForCurrentTenantAsync("App.General.DefaultLanguage", input.DefaultLanguage);
            await _settingManager.SetForCurrentTenantAsync("App.General.MaxExamAttempts", input.MaxExamAttempts.ToString());
            await _settingManager.SetForCurrentTenantAsync("App.General.LogoUrl", input.LogoUrl);
        }
    }
}