using System.IO;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Media.Dtos;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Media;

/// <summary>Question media and uploaded answers.</summary>
public interface IAssessmentMediaAppService : IApplicationService
{
    /// <summary>Validates and stores a file, returning the generated blob name.</summary>
    Task<MediaUploadResultDto> UploadAsync(IFormFile file);

    /// <summary>Reads a stored blob, or null when it does not exist.</summary>
    Task<Stream?> GetAsync(string blobName);

    Task DeleteAsync(string blobName);
}
