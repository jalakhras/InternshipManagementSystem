using System.IO;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Media;
using InternshipManagementSystem.Assessment.Media.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers.Assessment;

/// <summary>
/// Question media and uploaded answers.
/// <para>
/// The service behind this existed and was finished; the route did not. Five
/// places in the product build <c>/api/assessment/media/...</c> URLs — every
/// question image, every listening clip, every hotspot picture, every uploaded
/// answer — and all of them returned 404, upload included. Nothing in the test
/// suite crossed that line: the browser tests stub this exact URL, so they proved
/// the page renders what the server would send rather than that the server sends
/// it.
/// </para>
/// </summary>
[RemoteService(Name = "Assessment")]
[Area("assessment")]
[Route("api/assessment/media")]
public class AssessmentMediaController : AbpControllerBase
{
    /// <summary>
    /// The signed grant a candidate's browser carries, in the query string.
    /// <para>
    /// It has nowhere else to go. An image or an audio clip is fetched by the
    /// browser itself and no script gets to add a header to that request, so the
    /// address is the credential — which is why the grant names one blob and
    /// expires with the attempt rather than opening the container.
    /// </para>
    /// </summary>
    private const string GrantParameter = "grant";

    private readonly IAssessmentMediaAppService _media;

    public AssessmentMediaController(IAssessmentMediaAppService media)
    {
        _media = media;
    }

    /// <summary>Stores one file and returns the generated blob name.</summary>
    /// <remarks>
    /// The app service authorises this; it is not anonymous.
    /// </remarks>
    [HttpPost]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public Task<MediaUploadResultDto> UploadAsync(IFormFile file) => _media.UploadAsync(file);

    /// <summary>
    /// Serves a stored file to whoever is entitled to it.
    /// <para>
    /// Two kinds of caller, and they cannot be authorised the same way. Staff are
    /// signed in and are checked against the question permission. A candidate is
    /// not a user of this system at all and never becomes one, so they present the
    /// signed grant that came with their paper.
    /// </para>
    /// <para>
    /// Anonymous at the framework level and decided in the app service, which can
    /// see both branches. A caller entitled to neither gets 404 rather than 403:
    /// whether a particular blob exists is itself worth not saying.
    /// </para>
    /// </summary>
    [HttpGet("{**blobName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsync(string blobName)
    {
        var stream = await _media.GetAsync(blobName, Request.Query[GrantParameter].ToString());

        if (stream is null)
        {
            return NotFound();
        }

        // Inline, so a listening clip plays in the page rather than downloading, and
        // read-only: the browser is told exactly what this is and is not allowed to
        // sniff its way to something else.
        Response.Headers.XContentTypeOptions = "nosniff";

        return File(stream, ContentTypeFor(blobName));
    }

    /// <summary>Removes a stored file. Authorised by the app service.</summary>
    [HttpDelete("{**blobName}")]
    public Task DeleteAsync(string blobName) => _media.DeleteAsync(blobName);

    /// <summary>
    /// The content type, from the extension the upload allowlist already checked.
    /// <para>
    /// From the stored name rather than from anything the uploader declared. A
    /// browser's content type is caller input, and echoing it back is how a stored
    /// file becomes a stored script.
    /// </para>
    /// </summary>
    private static string ContentTypeFor(string blobName) =>
        Path.GetExtension(blobName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",

            // Deliberately not image/svg+xml. An SVG served as an image is a
            // document the browser will run script from, and these are shown inside
            // an exam that other people's answers pass through.
            ".svg" => "application/octet-stream",

            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",

            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",

            _ => "application/octet-stream",
        };
}
