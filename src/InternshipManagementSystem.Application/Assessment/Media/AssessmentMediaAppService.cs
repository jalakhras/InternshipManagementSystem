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
using Volo.Abp.Domain.Repositories;
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

    /// <summary>
    /// What an unauthenticated candidate may write, and of what kinds.
    /// <para>
    /// Smaller than the staff limit and a shorter list: this is the only door in
    /// the product an anonymous caller can push bytes through, and a recorded
    /// answer or a scanned page is a few megabytes. Audio is here because a
    /// spoken answer is recorded in the browser and arrives as webm or mp4
    /// depending on which browser it was.
    /// </para>
    /// </summary>
    private const long MaxAnswerSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedAnswerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".txt", ".png", ".jpg", ".jpeg",
        ".webm", ".mp4", ".m4a", ".ogg", ".mp3", ".wav"
    };

    private readonly IBlobContainer<AssessmentBlobContainer> _blobs;
    private readonly ExamSessionTokenService _sessions;
    private readonly ILogger<AssessmentMediaAppService> _logger;

    private readonly IRepository<Attempt, Guid> _attempts;

    public AssessmentMediaAppService(
        IBlobContainer<AssessmentBlobContainer> blobs,
        ExamSessionTokenService sessions,
        IRepository<Attempt, Guid> attempts,
        ILogger<AssessmentMediaAppService> logger)
    {
        _blobs = blobs;
        _sessions = sessions;
        _attempts = attempts;
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
    /// Stores a candidate's own answer file, authorised by their exam session.
    /// <para>
    /// Uploading was staff-only, so the two question types whose whole answer
    /// <i>is</i> a file — an uploaded document and a recorded spoken answer —
    /// could not be answered at all. A speaking test with no way to record is not
    /// a speaking test.
    /// </para>
    /// <para>
    /// Anonymous at the framework level and decided here, the same shape the read
    /// path uses, because a candidate is not a user of this system and never
    /// becomes one. The session token is the whole authorisation: it is signed,
    /// short-lived, minted for one attempt, and it stops being valid when that
    /// attempt is submitted.
    /// </para>
    /// <para>
    /// Tighter than the staff upload on purpose. This is the one place an
    /// unauthenticated stranger can write bytes to our disk, so it takes a
    /// smaller file, a shorter list of kinds, and files the blob under the
    /// attempt that produced it — which is also what makes an uploaded answer
    /// traceable to a sitting when somebody disputes a mark.
    /// </para>
    /// </summary>
    [AllowAnonymous]
    public async Task<MediaUploadResultDto> UploadAnswerAsync(IFormFile file, string sessionToken)
    {
        var claims = _sessions.Read(sessionToken)
            ?? throw new AbpAuthorizationException("The exam session is invalid or has expired.");

        if (claims.AttemptId == Guid.Empty)
        {
            // A session minted at the entry screen, before the attempt exists.
            // There is nothing to attach a file to yet.
            throw new AbpAuthorizationException("The exam has not been started.");
        }

        // The sitting has to still be open, and this is the check that makes a
        // stored file mean something.
        //
        // Without it the upload succeeded whatever the clock said. A candidate
        // recording an answer to a speaking question — talking, as people do,
        // until told to stop — had their recording accepted and written to
        // storage *after* the paper was submitted. The blob was real. Nothing
        // ever linked it to an answer, because the save that would have linked
        // it was refused for being late. So the recording sat on disk, the
        // marker's screen showed nothing, the attempt was marked as needing no
        // human at all, and the candidate was scored zero for an answer they had
        // given.
        //
        // Refused here, so a file that exists is a file that arrived in time.
        // The short grace on the other side is what keeps a genuine answer from
        // becoming the casualty of this fix.
        Attempt attempt;

        using (CurrentTenant.Change(claims.TenantId))
        {
            attempt = await _attempts.GetAsync(claims.AttemptId);
        }

        if (!attempt.IsWithinUploadGrace(Clock.Now))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.AttemptClosedForUploads);
        }

        if (file is null || file.Length == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.FileEmpty);
        }

        if (file.Length > MaxAnswerSizeBytes)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.FileTooLarge)
                .WithData("MaxMegabytes", MaxAnswerSizeBytes / (1024 * 1024));
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !AllowedAnswerExtensions.Contains(extension))
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.FileTypeNotAllowed)
                .WithData("Extension", extension ?? string.Empty);
        }

        // Nothing in the name comes from the caller: the tenant and the attempt
        // are read off the signed token, and the rest is generated.
        var blobName =
            $"{claims.TenantId?.ToString("N") ?? "host"}/answers/{claims.AttemptId:N}/{GuidGenerator.Create():N}{extension}";

        await using var stream = file.OpenReadStream();

        using (CurrentTenant.Change(claims.TenantId))
        {
            await _blobs.SaveAsync(blobName, stream);
        }

        _logger.LogInformation(
            "Stored answer blob {BlobName} for attempt {AttemptId} ({Bytes} bytes).",
            blobName, claims.AttemptId, file.Length);

        return new MediaUploadResultDto
        {
            BlobName = blobName,
            OriginalFileName = file.FileName,
            SizeInBytes = file.Length,
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

        var granted = _sessions.ReadMediaGrant(grant, blobName);

        if (granted is not null)
        {
            // Read as the tenant the grant names. A candidate has no tenant
            // context of their own — the link is their whole credential — so
            // without this the container looked under the host and every image on
            // a tenant's paper was a 404 with a perfectly valid grant.
            //
            // Safe because the grant is signed and names this exact blob: it can
            // only exist because the server put that file on somebody's paper.
            using (CurrentTenant.Change(granted.TenantId))
            {
                return await _blobs.GetOrNullAsync(blobName);
            }
        }

        // Staff, in their own tenant and nobody else's. Deliberately not the
        // tenant encoded in the name: an administrator who knew another
        // organisation's blob name could otherwise read it.
        if (await AuthorizationService.IsGrantedAsync(
                InternshipManagementSystemPermissions.Questions.Default))
        {
            return await _blobs.GetOrNullAsync(blobName);
        }

        // A marker may open what they are marking.
        //
        // Reading was guarded by a question permission, and the Marker role holds
        // none — it holds the three Review permissions and nothing else. So a
        // candidate who answered by uploading a file or recording themselves
        // arrived at a marker who could not open either. The paperclip rendered,
        // the href was empty, clicking it did nothing, and the marker was left to
        // put a number on work they had never seen.
        //
        // Narrowed to `answers/`, which is the only path an uploaded answer is
        // ever written to (`{tenant}/answers/{attemptId}/…`) and never where
        // question media goes. Granting `Questions.Default` instead would have
        // opened the question bank — including model answers — to a role that is
        // deliberately not allowed to see it.
        var answers = $"{CurrentTenant.Id?.ToString("N") ?? "host"}/answers/";

        if (blobName.StartsWith(answers, StringComparison.Ordinal)
            && await AuthorizationService.IsGrantedAsync(
                InternshipManagementSystemPermissions.Review.Grade))
        {
            return await _blobs.GetOrNullAsync(blobName);
        }

        return null;
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
