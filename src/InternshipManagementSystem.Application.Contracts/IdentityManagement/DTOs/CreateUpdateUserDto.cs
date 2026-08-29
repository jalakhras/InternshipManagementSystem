using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.IdentityManagement.DTOs
{
    public class CreateUpdateUserDto
    {
        [Required]
        [MaxLength(256)] // UserName غالباً 256
        public string UserName { get; set; }

        [Required]
        [MaxLength(256)] // Email غالباً 256
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// A password for a new account, or a replacement for an existing one.
        /// <para>
        /// Optional, and required at creation by <c>CreateAsync</c> instead of by
        /// an attribute. The same DTO carries both operations, and [Required]
        /// here made it impossible to edit an account at all without retyping a
        /// password: correcting a colleague's phone number answered 400, "The
        /// Password field is required", for a field the screen presents as
        /// optional when editing.
        /// </para>
        /// <para>
        /// Nullable deliberately. Nullable reference types are enabled in this
        /// project, and ASP.NET Core reads a non-nullable string as implicitly
        /// required — so leaving it <c>string</c> would reinstate exactly the
        /// rule being removed, with no attribute in sight to explain it.
        /// </para>
        /// </summary>
        [MaxLength(128)]
        public string? Password { get; set; }

        [MaxLength(256)]
        public string? FullName { get; set; } // الاسم الكامل للمستخدم

        /// <summary>
        /// Optional, and long enough for a real number.
        /// <para>
        /// It was capped at ten characters beside a comment saying sixteen, which
        /// rejects +966501234567 — thirteen — and every other number written with
        /// its country code.
        /// </para>
        /// </summary>
        [MaxLength(32)]
        [Phone]
        public string? PhoneNumber { get; set; } // رقم الهاتف

        /// <summary>
        /// The roles to give this account.
        /// <para>
        /// Set at creation rather than afterwards. An account created with none
        /// can sign in and see an empty application, and the person who created it
        /// has already moved on to telling them their password.
        /// </para>
        /// </summary>
        public List<string> Roles { get; set; } = new();
    }
}