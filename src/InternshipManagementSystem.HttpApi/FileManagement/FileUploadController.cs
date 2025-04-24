using InternshipManagementSystem.FileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers
{
    [Route("api/file-upload")]
    [Authorize] // تأكد أن فقط المستخدمين المسجلين يستطيعون الرفع، يمكنك تعديلها حسب الحاجة
    public class FileUploadController : AbpControllerBase
    {
        private readonly IFileUploadAppService _fileService;

        public FileUploadController(IFileUploadAppService fileUploadAppService)
        {
            _fileService = fileUploadAppService;
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        [HttpPost("upload-single")]
        public async Task<FileUploadResultDto> UploadFileAsync(IFormFile file, [FromQuery] string folder = "general")
        {
            if (file == null)
            {
                throw new AbpException("لم يتم رفع أي ملف.");
            }

            return await _fileService.UploadAsync(file, folder);
        }

        [HttpPost("upload-multiple")]
        public async Task<List<FileUploadResultDto>> UploadMultipleAsync([FromForm] List<IFormFile> files, [FromQuery] string folder)
        {
            return await _fileService.UploadMultipleAsync(files, folder);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFileAsync([FromQuery] string storedFileName, [FromQuery] string folder)
        {
            await _fileService.DeleteFileAsync(storedFileName, folder);
            return NoContent();
        }

        [HttpGet("get-url")]
        public async Task<string> GetFileUrlAsync([FromQuery] string storedFileName, [FromQuery] string folder)
        {
            return await _fileService.GetFileUrlAsync(storedFileName, folder);
        }
    }
}