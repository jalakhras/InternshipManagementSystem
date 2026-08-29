using InternshipManagementSystem.IdentityManagement.DTOs;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Identity;
// Both namespaces define these names. Volo's are the entities this service
// stores; ASP.NET's namespace is here only for the IdentityResult extension that
// turns a discarded failure into an exception.
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

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

            // Required here rather than by an attribute on the DTO, because the
            // same DTO is the update payload and an account being edited must not
            // have to carry a password it is not changing.
            if (string.IsNullOrWhiteSpace(input.Password))
            {
                throw new BusinessException(
                    InternshipManagementSystemDomainErrorCodes.UserPasswordRequired);
            }

            // Same reason: a rejected password produced no error at all, and the
            // caller was handed a user they could not sign in as.
            (await _userManager.CreateAsync(user, input.Password)).CheckErrors();

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

            await SetPasswordAsync(user, input.Password);

            await SetRolesAsync(user, input.Roles);

            await _userRepository.UpdateAsync(user);
            await CurrentUnitOfWork.SaveChangesAsync();

            return await ToDtoAsync(user);
        }

        /// <summary>
        /// Replaces the password, when one was typed.
        /// <para>
        /// The field was read on the way in, validated, carried through the DTO,
        /// and then dropped. An administrator resetting the password of somebody
        /// locked out typed a new one, pressed save, and was answered 200 — and
        /// the account kept the old password. There is no worse answer than that
        /// one: a refusal can be retried, and a lie is acted upon. They would go
        /// and read the new password out to a colleague who cannot sign in with
        /// it, and neither of them has anything to suspect.
        /// </para>
        /// <para>
        /// Remove-then-add rather than a reset token, because there is nobody to
        /// send a token to: this is an administrator setting a password on
        /// another person's account, not that person recovering their own.
        /// </para>
        /// <para>
        /// Blank means "leave it alone", which is what an empty password box on
        /// an edit form means to the person looking at it.
        /// </para>
        /// </summary>
        private async Task SetPasswordAsync(IdentityUser user, string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            (await _userManager.RemovePasswordAsync(user)).CheckErrors();
            (await _userManager.AddPasswordAsync(user, password)).CheckErrors();
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

            // CheckErrors, because a discarded IdentityResult means the caller is
            // told the save worked when the role was refused — a role that no
            // longer exists, or one the store would not write. Silence there is
            // worse than an error: the screen would show the truth on its next
            // load and nobody would know why it disagreed with what they did.
            foreach (var role in held.Where(r => !wanted.Contains(r)))
            {
                (await _userManager.RemoveFromRoleAsync(user, role)).CheckErrors();
            }

            foreach (var role in wanted.Where(r => !held.Contains(r)))
            {
                (await _userManager.AddToRoleAsync(user, role)).CheckErrors();
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
            // WithDetails, so each user arrives carrying its role links. Asking the
            // user manager per row cost one round trip per account on every load.
            var query = (await _userRepository.WithDetailsAsync()).OrderBy(u => u.UserName);

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToListAsync();

            // The links carry role ids; the screen shows names. One query for the
            // handful of roles an organisation has, rather than one per user.
            var roleNames = (await _roleRepository.GetListAsync())
                .ToDictionary(role => role.Id, role => role.Name);

            var rows = users.Select(user =>
            {
                var dto = ObjectMapper.Map<IdentityUser, UserDto>(user);

                dto.Roles = user.Roles
                    .Select(link => roleNames.GetValueOrDefault(link.RoleId))
                    .Where(name => name is not null)
                    .Select(name => name!)
                    .OrderBy(name => name)
                    .ToList();

                return dto;
            }).ToList();

            return new PagedResultDto<UserDto>(totalCount, rows);
        }
    }
}