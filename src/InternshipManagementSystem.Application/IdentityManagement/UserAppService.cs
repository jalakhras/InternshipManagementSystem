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

        public UserAppService(
            IRepository<IdentityUser, Guid> userRepository,
            IdentityUserManager userManager)
            : base(userRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
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

            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<IdentityUser, UserDto>(user);
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

            await _userRepository.UpdateAsync(user);
            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<IdentityUser, UserDto>(user);
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

            return new PagedResultDto<UserDto>(
                totalCount,
                ObjectMapper.Map<List<IdentityUser>, List<UserDto>>(users)
            );
        }
    }
}