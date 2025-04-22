using System.ComponentModel.DataAnnotations;
using Volo.Abp.Identity;

public class CreateSupervisorUserDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; }

    [Required]
    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(128)]
    public string Password { get; set; }

    [StringLength(10)]
    [Phone]
    public string PhoneNumber { get; set; }
}
