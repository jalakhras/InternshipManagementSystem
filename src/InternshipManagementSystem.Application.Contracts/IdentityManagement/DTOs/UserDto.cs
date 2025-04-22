using System;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.IdentityManagement
{
    public class UserDto : EntityDto<Guid>
    {
        public string UserName { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; } // الاسم الكامل

        public string PhoneNumber { get; set; } // رقم الهاتف
    }
}