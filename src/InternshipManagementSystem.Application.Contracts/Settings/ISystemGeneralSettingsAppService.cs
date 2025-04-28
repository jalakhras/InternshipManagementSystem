using InternshipManagementSystem.Settings.DTOs;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.SystemSettings
{
    public interface ISystemGeneralSettingsAppService : IApplicationService
    {
        public Task<GeneralSettingsDto> GetAsync();
        public Task UpdateAsync(UpdateGeneralSettingsDto input);
    }
}
