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

    /// <summary>
    /// Which permissions the seeder has already offered each of the other roles,
    /// recorded as <c>Role:Permission</c> pairs.
    /// <para>
    /// The same bookkeeping as <see cref="SeededPermissions"/> and for the same
    /// reason — grant once, never re-grant, so a deliberate revocation survives a
    /// deployment — but keyed by role as well as by permission. The coordinator
    /// and the author both hold <c>Exams.View</c>; one flat list of names would
    /// read the author's grant as proof the coordinator had already been offered
    /// it, and the coordinator would be seeded with a hole in it.
    /// </para>
    /// <para>
    /// Its own setting rather than more entries in the admin's, so the existing
    /// marker keeps its format and a deployment that has already run does not
    /// re-offer the admin everything on the way past.
    /// </para>
    /// </summary>
    public const string SeededRolePermissions = Prefix + ".Internal.SeededRolePermissions";

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

    /// <summary>
    /// Where a candidate goes when something goes wrong mid-exam.
    /// <para>
    /// Somebody's connection drops twenty minutes into a paper, or the recording
    /// will not start, or they cannot read the question. Until now the only
    /// address anywhere on their screen was ours — and they have no relationship
    /// with us at all. The centre invited them, the centre holds their result,
    /// and the centre is the only party that can do anything about it.
    /// </para>
    /// <para>
    /// Optional. An organisation that would rather not publish an address to
    /// candidates leaves it empty and nothing is shown, which is better than
    /// showing one nobody reads.
    /// </para>
    /// </summary>
    public const string SupportEmail = Prefix + ".SupportEmail";

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

}
