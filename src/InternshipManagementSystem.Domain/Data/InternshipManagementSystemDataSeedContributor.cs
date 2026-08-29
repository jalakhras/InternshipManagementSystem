using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.SettingManagement;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
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
        private readonly IPermissionManager _permissionManager;
        private readonly IPermissionDefinitionManager _permissionDefinitions;
        private readonly ISettingManager _settings;

        public InternshipManagementSystemDataSeedContributor(
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            IIdentityUserRepository userRepository,
            IIdentityRoleRepository roleRepository,
            IUnitOfWorkManager unitOfWorkManager,
            IGuidGenerator guidGenerator,
            PermissionManager permissionManager,
            IPermissionDefinitionManager permissionDefinitions,
            ISettingManager settings)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _guidGenerator = guidGenerator;
            _permissionManager = permissionManager;
            _permissionDefinitions = permissionDefinitions;
            _settings = settings;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            using var uow = _unitOfWorkManager.Begin();

            // 1. إنشاء الأدوار الأساسية
            await CreateRoleIfNotExistsAsync("Admin");
            await GrantAdminPanelAccessToAdminRoleAsync();

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

        /// <summary>
        /// Grants the admin role everything this application defines.
        /// <para>
        /// Read from the definition manager rather than listed here. A hardcoded
        /// list drifts the moment a permission is added, and the failure is
        /// invisible in tests: the admin simply gets a 403 on a screen that was
        /// working yesterday. That is exactly what happened — the seeder granted
        /// only Administration.Access, so every assessment screen returned 403
        /// and its loader never resolved.
        /// </para>
        /// <para>
        /// Scoped to this application's own group. ABP's own permissions
        /// (identity, tenant management, feature management) are seeded by their
        /// own modules, and granting them from here would silently override a
        /// deliberate revocation.
        /// </para>
        /// <para>
        /// Each permission is granted <i>once</i>, and the names already offered
        /// are remembered. Re-granting on every start looked idempotent and was
        /// not: an administrator who deliberately took Results.Export away from
        /// the admin role would find it back after the next deployment, with
        /// nothing to explain why. ABP's store cannot tell "revoked" from "never
        /// granted" — both are simply the absence of a row — so the record has to
        /// be kept here.
        /// </para>
        /// </summary>
        private async Task GrantAdminPanelAccessToAdminRoleAsync()
        {
            var adminRole = await _roleRepository.FindByNormalizedNameAsync("ADMIN");
            if (adminRole == null)
            {
                return;
            }

            var groups = await _permissionDefinitions.GetGroupsAsync();

            var ours = groups
                .Where(group => group.Name == InternshipManagementSystemPermissions.GroupName)
                .SelectMany(group => group.GetPermissionsWithChildren())
                .Select(permission => permission.Name)
                .ToList();

            var alreadyOffered = (await _settings.GetOrNullGlobalAsync(Settings.InternshipManagementSystemSettings.SeededPermissions) ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();

            var newlyDefined = ours.Where(name => !alreadyOffered.Contains(name)).ToList();

            foreach (var name in newlyDefined)
            {
                await _permissionManager.SetForRoleAsync(adminRole.Name, name, true);
            }

            if (newlyDefined.Count > 0)
            {
                // Written after granting, so a failure half way means the rest are
                // offered again on the next run rather than lost.
                await _settings.SetGlobalAsync(
                    Settings.InternshipManagementSystemSettings.SeededPermissions,
                    string.Join(',', alreadyOffered.Concat(newlyDefined).Distinct().OrderBy(n => n)));
            }
        }



    }
}