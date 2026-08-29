using InternshipManagementSystem.IdentityManagement.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// <summary>
        /// The roles an account can be given.
        /// <para>
        /// Read from the identity module rather than listed here, so a role added
        /// by an administrator appears without a deployment.
        /// </para>
        /// </summary>
        Task<List<string>> GetRolesAsync();
    }
}