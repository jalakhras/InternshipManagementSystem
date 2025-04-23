using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace InternshipManagementSystem.Data
{
    public class RoleDataSeederContributor : ITransientDependency
    {
        private readonly IIdentityRoleRepository _roleRepository;
        private readonly IGuidGenerator _guidGenerator;

        public RoleDataSeederContributor(IIdentityRoleRepository roleRepository, IGuidGenerator guidGenerator)
        {
            _roleRepository = roleRepository;
            _guidGenerator = guidGenerator;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            await CreateRoleIfNotExistsAsync("Admin");
            await CreateRoleIfNotExistsAsync("Supervisor");
            await CreateRoleIfNotExistsAsync("Trainee");
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