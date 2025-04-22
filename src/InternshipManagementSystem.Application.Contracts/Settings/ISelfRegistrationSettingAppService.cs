using System.Threading.Tasks;

namespace InternshipManagementSystem.Settings
{
    public interface ISelfRegistrationSettingAppService
    {
        Task<bool> IsSelfRegistrationEnabledAsync();
    }
}