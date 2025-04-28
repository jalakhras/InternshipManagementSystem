using InternshipManagementSystem.Settings.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.SystemSettings
{
    [RemoteService(Name = "InternshipManagementSystem")]
    [Route("api/system-settings/general")]
    [Authorize] // (يمكن تخصيص صلاحيات لاحقًا لو أردت)
    public class SystemGeneralSettingsController : AbpControllerBase
    {
        private readonly ISystemGeneralSettingsAppService _generalSettingsAppService;

        public SystemGeneralSettingsController(ISystemGeneralSettingsAppService generalSettingsAppService)
        {
            _generalSettingsAppService = generalSettingsAppService;
        }

        [HttpGet]
        public async Task<GeneralSettingsDto> GetAsync()
        {
            return await _generalSettingsAppService.GetAsync();
        }

        [HttpPut]
        public async Task UpdateAsync(UpdateGeneralSettingsDto input)
        {
            await _generalSettingsAppService.UpdateAsync(input);
        }
    }
}