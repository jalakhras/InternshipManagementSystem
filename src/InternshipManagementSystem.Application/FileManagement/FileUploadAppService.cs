using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.FileManagement
{
    public class FileUploadAppService : ApplicationService, IFileUploadAppService
    {
        private readonly string _rootUploadPath;
        private readonly ILogger<FileUploadAppService> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".mp3", ".mp4", ".docx", ".txt" };
        private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

        public FileUploadAppService(IConfiguration configuration, ILogger<FileUploadAppService> logger, IHostingEnvironment hostingEnvironment)
        {
            _rootUploadPath = configuration["App:UploadRootPath"] ?? "wwwroot/uploads";
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<FileUploadResultDto> UploadAsync(IFormFile file, string folder = "general")
        {
            if (file == null || file.Length == 0)
                throw new UserFriendlyException("الملف فارغ!");

            if (file.Length > MaxFileSizeInBytes)
                throw new UserFriendlyException($"حجم الملف أكبر من الحد المسموح به ({MaxFileSizeInBytes / (1024 * 1024)} ميجابايت).");

            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension))
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
            var fullPath = Path.Combine(_rootUploadPath, folder, storedFileName);
            await DeleteFileByPathAsync(fullPath);
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            var relativePath = fileUrl.StartsWith("/") ? fileUrl.Substring(1) : fileUrl;
            var fullPath = Path.Combine(_hostingEnvironment.WebRootPath, relativePath);

            await DeleteFileByPathAsync(fullPath);
        }

        public async Task<string> GetFileUrlAsync(string storedFileName, string folder = "general")
        {
            var url = $"/uploads/{folder}/{storedFileName}".Replace("\\", "/");
            return await Task.FromResult(url);
        }

        // 🔥 دالة مساعدة لحذف أي ملف عبر المسار الكامل
        private async Task DeleteFileByPathAsync(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation($"تم حذف الملف: {fullPath}");
            }
            else
            {
                _logger.LogWarning($"لم يتم العثور على الملف للحذف: {fullPath}");
            }

            await Task.CompletedTask;
        }
    }
}