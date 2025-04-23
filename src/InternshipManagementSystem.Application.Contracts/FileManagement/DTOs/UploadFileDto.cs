using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.FileManagement.DTOs
{
    public class UploadFileDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}