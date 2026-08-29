using Volo.Abp.BlobStoring;

namespace InternshipManagementSystem.Assessment;

/// <summary>
/// Where assessment media and uploaded answers live.
/// <para>
/// Blob storing replaces the old file service, which joined a caller-supplied folder
/// name straight into a filesystem path — so <c>../../</c> in that argument wrote
/// wherever the process could reach. It also read its root from a config key that was
/// set to an empty string, and an empty string is not null, so the <c>??</c> fallback
/// never fired and uploads landed in the working directory.
/// </para>
/// <para>
/// Container names are fixed constants, never caller input. Blob names are generated.
/// Moving to S3 or Azure later is a configuration change, not a rewrite.
/// </para>
/// </summary>
[BlobContainerName(Name)]
public class AssessmentBlobContainer
{
    public const string Name = "assessment";
}
