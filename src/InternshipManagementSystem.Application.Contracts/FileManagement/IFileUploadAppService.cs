using InternshipManagementSystem.FileManagement.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternshipManagementSystem.FileManagement
{
    public interface IFileUploadAppService
    {
        Task<FileUploadResultDto> UploadAsync(IFormFile file, string folder = "general");
        Task<List<FileUploadResultDto>> UploadMultipleAsync(List<IFormFile> files, string folder = "general");
        Task DeleteFileAsync(string storedFileName, string folder = "general");
        Task<string> GetFileUrlAsync(string storedFileName, string folder = "general");
    }
}
