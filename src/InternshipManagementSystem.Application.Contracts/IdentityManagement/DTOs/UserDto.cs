using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.IdentityManagement
{
    public class UserDto : EntityDto<Guid>
    {
        public string UserName { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; } // الاسم الكامل

        public string PhoneNumber { get; set; } // رقم الهاتف

        /// <summary>
        /// The roles this account holds.
        /// <para>
        /// Carried on the row rather than fetched per user, because "who can do
        /// what" is the question this screen exists to answer and a list of names
        /// without it answers nothing.
        /// </para>
        /// </summary>
        public List<string> Roles { get; set; } = new();
    }
}