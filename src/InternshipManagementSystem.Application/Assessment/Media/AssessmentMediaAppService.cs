using System;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Media.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;

namespace InternshipManagementSystem.Assessment.Media;

/// <summary>
/// Stores question media and uploaded answers.
/// <para>
/// Replaces the old FileUploadAppService, which had three problems: it joined a
/// caller-supplied folder name into a filesystem path without sanitising it, so
/// <c>../../</c> escaped the upload directory; it read its root from a config key set
/// to an empty string, which the <c>??</c> fallback did not catch, so files landed in
/// the process working directory; and it tied the product to local disk.
/// </para>
/// <para>
/// Here the container is a constant, the blob name is generated from a GUID, and the
/// caller's filename is kept only as a label. There is no path the caller can steer.
/// </para>
/// </summary>
/// <remarks>
/// Every other assessment service carries a class-level <c>[Authorize]</c>; this
/// one was missed, and ABP registers conventional controllers for the whole
/// assembly. All three methods were anonymous HTTP endpoints — read any blob by
/// name forever, or delete the stimulus image out of a live exam.
/// </remarks>
[Authorize]
public class AssessmentMediaAppService : ApplicationService, IAssessmentMediaAppService
{
    /// <summary>
    /// Extensions we are willing to hold. An allowlist, because a denylist is a
    /// guess about every format that might ever be dangerous.
    /// </summary>
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images: question stimuli, charts, diagrams
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg",
        // Audio: listening comprehension, spoken answers
        ".mp3", ".wav", ".m4a", ".ogg", ".webm",
        // Video
        ".mp4", ".mov",
        // Documents: uploaded answers
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".zip"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private readonly IBlobContainer<AssessmentBlobContainer> _blobs;
    private readonly ExamSessionTokenService _sessions;
    private readonly ILogger<AssessmentMediaAppService> _logger;

    public AssessmentMediaAppService(
        IBlobContainer<AssessmentBlobContainer> blobs,
        ExamSessionTokenService sessions,
        ILogger<AssessmentMediaAppService> logger)
    {
        _blobs = blobs;
        _sessions = sessions;
        _logger = logger;
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Edit)]
    public async Task<MediaUploadResultDto> UploadAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.FileEmpty);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.FileTooLarge)
                .WithData("MaxMegabytes", MaxFileSizeBytes / (1024 * 1024));
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.FileTypeNotAllowed)
                .WithData("Extension", extension ?? string.Empty);
        }

        // The name is generated, and the tenant prefix keeps one tenant's blobs from
        // colliding with another's. Nothing here derives from caller input.
        var blobName = $"{CurrentTenant.Id?.ToString("N") ?? "host"}/{GuidGenerator.Create():N}{extension.ToLowerInvariant()}";

        await using var stream = file.OpenReadStream();
        await _blobs.SaveAsync(blobName, stream);

        _logger.LogInformation("Stored blob {BlobName} ({Bytes} bytes).", blobName, file.Length);

        return new MediaUploadResultDto
        {
            BlobName = blobName,
            // Kept for display only. It is never used to build a path.
            OriginalFileName = Path.GetFileName(file.FileName),
            MediaType = ClassifyByExtension(extension),
            SizeInBytes = file.Length,
            Url = $"/api/assessment/media/{blobName}"
        };
    }

    /// <summary>
    /// Reads a stored blob for a caller entitled to it.
    /// <para>
    /// Two kinds of caller, and they cannot be authorised the same way. Staff are
    /// signed in and are checked against the question permission. A candidate
    /// sitting an exam is not a user of this system and never becomes one, so they
    /// present the signed grant that came with their paper — which names one blob
    /// and expires with the attempt.
    /// </para>
    /// <para>
    /// Anonymous at the attribute level because neither branch can be written as
    /// one, and a caller with neither gets the same answer as a caller asking for
    /// something that is not there. Whether a given blob exists is itself worth not
    /// saying.
    /// </para>
    /// </summary>
    [AllowAnonymous]
    public async Task<Stream?> GetAsync(string blobName, string? grant = null)
    {
        // Rejects any traversal attempt on the read side too, since blob names travel
        // through URLs and a stored name is not automatically a trusted one.
        if (blobName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(blobName))
        {
            throw new AbpAuthorizationException("Invalid blob name.");
        }

        var entitled = _sessions.GrantsMedia(grant, blobName)
                       || await AuthorizationService.IsGrantedAsync(
                           InternshipManagementSystemPermissions.Questions.Default);

        if (!entitled)
        {
            return null;
        }

        return await _blobs.GetOrNullAsync(blobName);
    }

    [Authorize(InternshipManagementSystemPermissions.Questions.Edit)]
    public async Task DeleteAsync(string blobName)
    {
        // The same guard the read side has. It was missing here, so the one method
        // that destroys something was the one that accepted a traversal — which is
        // the wrong way round for the two of them to differ.
        if (blobName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(blobName))
        {
            throw new AbpAuthorizationException("Invalid blob name.");
        }

        await _blobs.DeleteAsync(blobName);
    }

    /// <summary>
    /// What kind of thing this is, from its extension.
    /// <para>
    /// Decided here rather than trusted from the request: the browser's declared
    /// content type is caller input, and a question that renders a "picture" the
    /// server never verified is a question rendering whatever was uploaded. The
    /// extension itself is already checked against the allowlist above, so by the
    /// time this runs the set is closed.
    /// </para>
    /// </summary>
    private static string ClassifyByExtension(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".svg" => "image",
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".aac" => "audio",
            ".mp4" or ".webm" or ".mov" or ".m4v" => "video",
            _ => "document",
        };
}
