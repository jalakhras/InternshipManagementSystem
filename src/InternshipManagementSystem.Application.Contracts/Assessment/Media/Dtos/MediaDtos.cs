namespace InternshipManagementSystem.Assessment.Media.Dtos;

/// <summary>What a stored upload became.</summary>
public class MediaUploadResultDto
{
    /// <summary>Generated identifier for the blob. This is what entities reference.</summary>
    public string BlobName { get; set; } = default!;

    /// <summary>The name the uploader's file had. Kept as a label; never used as a path.</summary>
    public string OriginalFileName { get; set; } = default!;

    public long SizeInBytes { get; set; }

    /// <summary>Where to fetch it.</summary>
    public string Url { get; set; } = default!;
}
