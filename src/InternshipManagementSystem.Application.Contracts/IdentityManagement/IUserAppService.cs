using InternshipManagementSystem.IdentityManagement.DTOs;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.IdentityManagement
{
    public interface IUserAppService :
        ICrudAppService<
            UserDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateUserDto,
            CreateUpdateUserDto>
    {
    }
}