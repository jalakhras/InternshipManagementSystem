using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

public class CreateTraineeUserDto : AuditedEntityDto<Guid>
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

    [Required]
    public Guid SpecializationId { get; set; } // تخصص المتدرب

    [StringLength(10)]
    [Phone]
    public string PhoneNumber { get; set; }
}