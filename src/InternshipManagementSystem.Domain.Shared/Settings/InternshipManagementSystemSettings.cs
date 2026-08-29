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

    // ---- Bookkeeping ----

    /// <summary>
    /// Which permissions the seeder has already offered the admin role.
    /// <para>
    /// Not a setting anyone changes and not shown anywhere: a marker, so that
    /// granting is a one-time act per permission. Re-granting on every start
    /// looked idempotent and was not — an administrator who deliberately took a
    /// permission away from the admin role would find it back after the next
    /// deployment, with nothing to explain why. ABP's store cannot tell "revoked"
    /// from "never granted", so the record has to live here.
    /// </para>
    /// </summary>
    public const string SeededPermissions = Prefix + ".Internal.SeededPermissions";

    // ---- Presentation ----

    /// <summary>
    /// The language a tenant's people get before they pick one. Arabic by default;
    /// a tenant serving English speakers changes it here, not in a build.
    /// </summary>
    public const string DefaultLanguage = Prefix + ".DefaultLanguage";

    /// <summary>Tenant timezone. The exam timer and every scheduled window depend on it.</summary>
    public const string TimeZone = Prefix + ".TimeZone";

    /// <summary>
    /// What this organisation calls itself.
    /// <para>
    /// Shown in the shell, on the screen a candidate opens their link on, and in
    /// the invitation email. A candidate sitting an English placement test for a
    /// language centre should see that centre's name, not ours: they have no
    /// relationship with us and no reason to trust a name they have never heard.
    /// </para>
    /// </summary>
    public const string OrganizationName = Prefix + ".OrganizationName";

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
