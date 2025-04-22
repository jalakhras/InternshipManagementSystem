using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.IdentityManagement.DTOs
{
    namespace InternshipManagementSystem.IdentityManagement.DTOs
    {
        public class RegisterUserDto
        {
            [Required]
            [StringLength(128)]
            public string UserName { get; set; }

            [Required]
            [StringLength(256)]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(128, MinimumLength = 6)]
            public string Password { get; set; }

            [Required]
            [StringLength(256)]
            public string FullName { get; set; }

            [Required]
            public UserType UserType { get; set; } 
        }

        public enum UserType
        {
            Trainee,
            Supervisor
        }
    }


}