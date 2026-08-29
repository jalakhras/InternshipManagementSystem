using InternshipManagementSystem.IdentityManagement.DTOs;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace InternshipManagementSystem.IdentityManagement
{
    [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Default)]
    public class UserAppService :
        CrudAppService<
            IdentityUser,
            UserDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateUserDto>,
        IUserAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<IdentityRole, Guid> _roleRepository;

        public UserAppService(
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<IdentityRole, Guid> roleRepository,
            IdentityUserManager userManager)
            : base(userRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<List<string>> GetRolesAsync()
        {
            var roles = await _roleRepository.GetListAsync();

            return roles.Select(r => r.Name).OrderBy(name => name).ToList();
        }

        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Create)]
        public override async Task<UserDto> CreateAsync(CreateUpdateUserDto input)
        {
            var user = new IdentityUser(
                GuidGenerator.Create(),
                input.UserName,
                input.Email,
                CurrentTenant.Id
            );

            user.Name = input.FullName;
            user.Surname = string.Empty;

            await _userManager.CreateAsync(user, input.Password);

            if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
            {
                await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);
            }

            // Before saving, because an account created with no role can sign in
            // and see an empty application — and by then whoever created it has
            // moved on to telling them their password.
            await SetRolesAsync(user, input.Roles);

            await CurrentUnitOfWork.SaveChangesAsync();

            return await ToDtoAsync(user);
        }

        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Edit)]
        public override async Task<UserDto> UpdateAsync(Guid id, CreateUpdateUserDto input)
        {
            var user = await _userRepository.GetAsync(id);

            if (user.UserName != input.UserName)
            {
                await _userManager.SetUserNameAsync(user, input.UserName);
            }

            if (user.Email != input.Email)
            {
                await _userManager.SetEmailAsync(user, input.Email);
            }

            user.Name = input.FullName;
            user.Surname = string.Empty;

            if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
            {
                await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber);
            }

            await SetRolesAsync(user, input.Roles);

            await _userRepository.UpdateAsync(user);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await ToDtoAsync(user);
        }

        /// <summary>
        /// Makes the account's roles match the list exactly.
        /// <para>
        /// Whole-list rather than add-and-remove, because the screen shows every
        /// role with the held ones ticked, and what is ticked when they press save
        /// is what they mean.
        /// </para>
        /// <para>
        /// Guarded separately from editing. <c>Users.ManageRoles</c> existed,
        /// appeared in the permission tree and was enforced nowhere — so anybody
        /// who could correct a colleague's phone number could also tick Admin on
        /// their own record. Deciding what an account may do is a different act
        /// from maintaining its details, which is what the two permissions have
        /// always claimed.
        /// </para>
        /// <para>
        /// Checked only when the list actually differs. Saving a profile without
        /// touching the roles must not require a permission the person does not
        /// need, or the two permissions collapse back into one.
        /// </para>
        /// </summary>
        private async Task SetRolesAsync(IdentityUser user, List<string> roles)
        {
            var wanted = (roles ?? new List<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            var held = await _userManager.GetRolesAsync(user);

            if (!wanted.OrderBy(r => r).SequenceEqual(held.OrderBy(r => r)))
            {
                await AuthorizationService.CheckAsync(
                    InternshipManagementSystemPermissions.IdentityManagement.Users.ManageRoles);
            }

            foreach (var role in held.Where(r => !wanted.Contains(r)))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }

            foreach (var role in wanted.Where(r => !held.Contains(r)))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        /// <summary>The row a screen renders, with the roles it holds.</summary>
        private async Task<UserDto> ToDtoAsync(IdentityUser user)
        {
            var dto = ObjectMapper.Map<IdentityUser, UserDto>(user);
            dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return dto;
        }

        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Delete)]
        public override async Task DeleteAsync(Guid id)
        {
            await _userManager.DeleteAsync(await _userRepository.GetAsync(id));
        }

        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.View)]
        public override async Task<PagedResultDto<UserDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var query = await _userRepository.GetQueryableAsync();

            query = query.OrderBy(u => u.UserName); // ترتيب بالاسم

            var totalCount = query.Count();

            var users = query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            var rows = new List<UserDto>();

            foreach (var user in users)
            {
                rows.Add(await ToDtoAsync(user));
            }

            return new PagedResultDto<UserDto>(totalCount, rows);
        }
    }
}