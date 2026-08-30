using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace InternshipManagementSystem.Assessment.Settings;

/// <summary>
/// What an organisation changes about the platform for itself.
/// <para>
/// Every value here is per-tenant. A recruitment firm, a language centre and a
/// trading academy share one deployment and none of them should be working in a
/// system that looks like it was built for somebody else.
/// </para>
/// </summary>
public interface ITenantSettingsAppService : IApplicationService
{
    /// <summary>
    /// Reads the current values.
    /// <para>
    /// Readable by anyone signed in, because the shell needs the name and the mark
    /// to render its own header — hiding branding behind an administrative
    /// permission would leave everybody else looking at a blank corner.
    /// </para>
    /// </summary>
    Task<TenantSettingsDto> GetAsync();

    Task<TenantSettingsDto> UpdateAsync(TenantSettingsDto input);
}

public class TenantSettingsDto
{
    /// <summary>
    /// What this organisation calls itself.
    /// <para>
    /// Shown to candidates as well as staff. Somebody opening a placement test
    /// link has no relationship with us and no reason to trust a name they have
    /// never heard.
    /// </para>
    /// </summary>
    [StringLength(128)]
    public string? OrganizationName { get; set; }

    /// <summary>The organisation's mark, as a stored blob name.</summary>
    [StringLength(256)]
    public string? LogoBlobName { get; set; }

    /// <summary>
    /// An accent colour, as a hex value, applied over the design tokens.
    /// <para>
    /// One colour rather than a palette. A tenant that can set every colour can
    /// set an unreadable one, and the contrast of the rest of the system is not
    /// theirs to break.
    /// </para>
    /// </summary>
    [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    public string? BrandColor { get; set; }

    /// <summary>The language people here get before they choose one.</summary>
    [StringLength(16)]
    public string? DefaultLanguage { get; set; }

    /// <summary>The zone every exam clock and scheduled window is read in.</summary>
    [StringLength(64)]
    public string? TimeZone { get; set; }

    /// <summary>Pass mark applied to a new exam unless its author overrides it.</summary>
    [Range(1, 100)]
    public decimal DefaultPassingPercentage { get; set; }

    /// <summary>Whether a candidate sees their result as soon as grading finishes.</summary>
    public bool ShowResultToCandidate { get; set; }

    /// <summary>
    /// Record paste, focus-loss and timing observations during attempts.
    /// <para>
    /// A centre running low-stakes practice may reasonably switch this off, and
    /// in some jurisdictions recording it without telling people is not theirs to
    /// decide.
    /// </para>
    /// </summary>
    public bool CollectIntegritySignals { get; set; }

    /// <summary>Whether people may create their own accounts.</summary>
}
