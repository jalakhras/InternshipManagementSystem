using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings; // <-- مهم جدًا إضافته لاستخدام ISettingManager

namespace InternshipManagementSystem.Data
{
    public class RoleDataSeederContributor : ITransientDependency
    {
        private readonly IIdentityRoleRepository _roleRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ISettingManager _settingManager;

        public RoleDataSeederContributor(
            IIdentityRoleRepository roleRepository,
            IGuidGenerator guidGenerator,
            ISettingManager settingManager)
        {
            _roleRepository = roleRepository;
            _guidGenerator = guidGenerator;
            _settingManager = settingManager;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            await CreateRoleIfNotExistsAsync("Admin");
            await CreateRoleIfNotExistsAsync("Supervisor");
            await CreateRoleIfNotExistsAsync("Trainee");

            await _settingManager.SetForCurrentTenantAsync("App.General.SiteName", "Internship Management System");
            await _settingManager.SetForCurrentTenantAsync("App.General.DefaultLanguage", "en");
            await _settingManager.SetForCurrentTenantAsync("App.General.MaxExamAttempts", "1");
            await _settingManager.SetForCurrentTenantAsync("App.General.LogoUrl", "");

        }

        private async Task CreateRoleIfNotExistsAsync(string roleName)
        {
            var existingRole = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());

            if (existingRole == null)
            {
                var role = new IdentityRole(_guidGenerator.Create(), roleName)
                {
                    IsDefault = false,
                    IsStatic = true
                };

                await _roleRepository.InsertAsync(role, autoSave: true);
            }
        }
    }
}
