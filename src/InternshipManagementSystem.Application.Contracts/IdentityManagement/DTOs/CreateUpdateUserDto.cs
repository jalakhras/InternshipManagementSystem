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

        [Required]
        [MaxLength(128)] // Password غالباً 128
        public string Password { get; set; }

        [MaxLength(256)] // Email غالباً 256
        public string FullName { get; set; } // الاسم الكامل للمستخدم

        [MaxLength(10)] // PhoneNumber غالباً 16
        [Phone]
        public string PhoneNumber { get; set; } // رقم الهاتف
    }
}