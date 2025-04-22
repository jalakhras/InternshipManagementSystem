using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using InternshipManagementSystem.Permissions;
using InternshipManagementSystem.IdentityManagement;
using Asp.Versioning;
using InternshipManagementSystem.IdentityManagement.DTOs;

namespace InternshipManagementSystem.Controllers.IdentityManagement
{
    [RemoteService]
    [Area("app")]
    [ControllerName("User")]
    [Route("api/app/users")]
    [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Default)]
    public class UserController : AbpController, IUserAppService
    {
        private readonly IUserAppService _userAppService;

        public UserController(IUserAppService userAppService)
        {
            _userAppService = userAppService;
        }

        [HttpGet]
        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.View)]
        public Task<PagedResultDto<UserDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            return _userAppService.GetListAsync(input);
        }

        [HttpGet("{id}")]
        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.View)]
        public Task<UserDto> GetAsync(Guid id)
        {
            return _userAppService.GetAsync(id);
        }

        [HttpPost]
        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Create)]
        public Task<UserDto> CreateAsync(CreateUpdateUserDto input)
        {
            return _userAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Edit)]
        public Task<UserDto> UpdateAsync(Guid id, CreateUpdateUserDto input)
        {
            return _userAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        [Authorize(InternshipManagementSystemPermissions.IdentityManagement.Users.Delete)]
        public Task DeleteAsync(Guid id)
        {
            return _userAppService.DeleteAsync(id);
        }
    }
}
