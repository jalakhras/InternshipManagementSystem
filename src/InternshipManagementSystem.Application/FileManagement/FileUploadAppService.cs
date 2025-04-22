using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.FileManagement
{
    public class FileUploadAppService : ApplicationService, IFileUploadAppService
    {
        private readonly string _rootUploadPath;
        private readonly ILogger<FileUploadAppService> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".mp3", ".mp4", ".docx", ".txt" };
        private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

        public FileUploadAppService(IConfiguration configuration, ILogger<FileUploadAppService> logger)
        {
            _rootUploadPath = configuration["App:UploadRootPath"] ?? "wwwroot/uploads";
            _logger = logger;
        }

        public async Task<FileUploadResultDto> UploadAsync(IFormFile file, string folder = "general")
        {
            if (file == null || file.Length == 0)
                throw new UserFriendlyException("الملف فارغ!");

            if (file.Length > MaxFileSizeInBytes)
                throw new UserFriendlyException($"حجم الملف أكبر من الحد المسموح به ({MaxFileSizeInBytes / (1024 * 1024)} ميجابايت).");

            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !Array.Exists(AllowedExtensions, ext => ext == fileExtension))
                throw new UserFriendlyException("صيغة الملف غير مدعومة!");

            var fileName = $"{Guid.NewGuid():N}{fileExtension}";
            var uploadPath = Path.Combine(_rootUploadPath, folder);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation($"تم رفع الملف بنجاح: {filePath}");

            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(filePath, out string contentType);

            return new FileUploadResultDto
            {
                FileName = fileName,
                FilePath = $"/uploads/{folder}/{fileName}".Replace("\\", "/"),
                Size = file.Length,
                ContentType = contentType ?? "application/octet-stream"
            };
        }

        public async Task<List<FileUploadResultDto>> UploadMultipleAsync(List<IFormFile> files, string folder = "general")
        {
            var results = new List<FileUploadResultDto>();

            foreach (var file in files)
            {
                var result = await UploadAsync(file, folder);
                results.Add(result);
            }

            return results;
        }
        public async Task DeleteFileAsync(string storedFileName, string folder = "general")
        {
            var filePath = Path.Combine(_rootUploadPath, folder, storedFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            await Task.CompletedTask;
        }
        public async Task<string> GetFileUrlAsync(string storedFileName, string folder = "general")
        {
            var url = $"/uploads/{folder}/{storedFileName}";
            return await Task.FromResult(url);
        }


    }
}
