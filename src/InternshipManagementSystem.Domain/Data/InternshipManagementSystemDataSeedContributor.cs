using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace InternshipManagementSystem
{
    public class InternshipManagementSystemDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IdentityUserManager _userManager;
        private readonly IdentityRoleManager _roleManager;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IIdentityRoleRepository _roleRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IGuidGenerator _guidGenerator;

        public InternshipManagementSystemDataSeedContributor(
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            IIdentityUserRepository userRepository,
            IIdentityRoleRepository roleRepository,
            IUnitOfWorkManager unitOfWorkManager,
            IGuidGenerator guidGenerator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _guidGenerator = guidGenerator;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            using var uow = _unitOfWorkManager.Begin();

            // 1. إنشاء الأدوار الأساسية
            await CreateRoleIfNotExistsAsync("Admin");
            await CreateRoleIfNotExistsAsync("Supervisor");
            await CreateRoleIfNotExistsAsync("Trainee");

            // 2. إنشاء المستخدمين الأساسيين
            await CreateUserIfNotExistsAsync("admin@internship.com", "123456Aa@", "Admin");
            await CreateUserIfNotExistsAsync("Jassar1994@gmail.com", "123456Aa@", "Admin");
            await CreateUserIfNotExistsAsync("Supervisor@internship.com", "123456Aa@", "Supervisor");
            await CreateUserIfNotExistsAsync("Trainee@internship.com", "123456Aa@", "Trainee");

            await uow.CompleteAsync();
        }

        private async Task CreateRoleIfNotExistsAsync(string roleName)
        {
            var normalizedRoleName = roleName.ToUpperInvariant();
            var existingRole = await _roleRepository.FindByNormalizedNameAsync(normalizedRoleName);
            if (existingRole != null)
            {
                return; // موجود بالفعل - لا تفعل شيء
            }

            var newRole = new IdentityRole(_guidGenerator.Create(), roleName);
            await _roleManager.CreateAsync(newRole);
        }

        private async Task CreateUserIfNotExistsAsync(string email, string password, string roleName)
        {
            var normalizedEmail = email.ToUpperInvariant();
            var existingUser = await _userRepository.FindByNormalizedUserNameAsync(normalizedEmail);

            if (existingUser != null)
            {
                return; // موجود بالفعل - لا تفعل شيء
            }

            var user = new IdentityUser(_guidGenerator.Create(), email, email);
            (await _userManager.CreateAsync(user, password)).CheckErrors();

            var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());
            if (role != null)
            {
                (await _userManager.AddToRoleAsync(user, role.Name)).CheckErrors();
            }
        }
    }
}