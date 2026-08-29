namespace InternshipManagementSystem.Assessment.Media.Dtos;

/// <summary>What a stored upload became.</summary>
public class MediaUploadResultDto
{
    /// <summary>Generated identifier for the blob. This is what entities reference.</summary>
    public string BlobName { get; set; } = default!;

    /// <summary>The name the uploader's file had. Kept as a label; never used as a path.</summary>
    public string OriginalFileName { get; set; } = default!;

    public long SizeInBytes { get; set; }

    /// <summary>
    /// image, audio, video or document — decided from the extension by the server,
    /// which is the only party that saw the file.
    /// <para>
    /// Returned so the caller can store it beside the blob name without inspecting
    /// the file itself. A question needs it to know whether to render a picture, a
    /// player or a download.
    /// </para>
    /// </summary>
    public string MediaType { get; set; } = default!;

    /// <summary>Where to fetch it.</summary>
    public string Url { get; set; } = default!;
}
