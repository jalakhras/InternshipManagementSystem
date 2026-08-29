namespace InternshipManagementSystem.Settings;

/// <summary>
/// Settings a tenant changes for itself.
/// <para>
/// These are what let one platform serve a recruitment firm, a language school and
/// a trading academy without any of them working in a system built for someone else.
/// </para>
/// </summary>
public static class InternshipManagementSystemSettings
{
    private const string Prefix = "Assessment";

    // ---- Presentation ----

    /// <summary>
    /// The language a tenant's people get before they pick one. Arabic by default;
    /// a tenant serving English speakers changes it here, not in a build.
    /// </summary>
    public const string DefaultLanguage = Prefix + ".DefaultLanguage";

    /// <summary>Tenant timezone. The exam timer and every scheduled window depend on it.</summary>
    public const string TimeZone = Prefix + ".TimeZone";

    /// <summary>Tenant logo, as a blob name. Shown in the shell and on emailed invitations.</summary>
    public const string LogoBlobName = Prefix + ".LogoBlobName";

    /// <summary>Brand accent colour as a hex value, applied over the design tokens.</summary>
    public const string BrandColor = Prefix + ".BrandColor";

    // ---- Assessment defaults ----

    /// <summary>Pass mark applied to a new exam unless its author overrides it.</summary>
    public const string DefaultPassingPercentage = Prefix + ".DefaultPassingPercentage";

    /// <summary>Whether a taker sees their result as soon as grading completes.</summary>
    public const string ShowResultToCandidate = Prefix + ".ShowResultToCandidate";

    // ---- Integrity ----

    /// <summary>
    /// Record paste, focus-loss and timing observations during attempts. A tenant
    /// running low-stakes practice may reasonably switch this off.
    /// </summary>
    public const string CollectIntegritySignals = Prefix + ".CollectIntegritySignals";

    // ---- Access ----

    /// <summary>Whether people may create their own accounts.</summary>
    public const string EnableSelfRegistration = Prefix + ".EnableSelfRegistration";
}
