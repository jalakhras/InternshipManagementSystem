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

    /// <summary>
    /// Reads a stored blob, or null when it does not exist or the caller is not
    /// entitled to it.
    /// </summary>
    /// <param name="blobName">The stored name, as it came back from the upload.</param>
    /// <param name="grant">
    /// The signed grant a candidate's paper carried, when there is one. Staff have
    /// no grant and are checked against their permissions instead.
    /// </param>
    Task<Stream?> GetAsync(string blobName, string? grant = null);

    Task DeleteAsync(string blobName);
}
