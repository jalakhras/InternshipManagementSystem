using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Tenancy;

/// <summary>
/// How a tenant appears to its own people.
/// <para>
/// A candidate invited to sit an exam is being asked to trust the organisation
/// that invited them, not us. If the page they land on carries our name, the
/// invitation looks like a phishing attempt — which is the practical reason
/// branding is not decoration here, and why it reaches the exam page, the
/// certificate and the invitation email rather than only the admin shell.
/// </para>
/// <para>
/// Deliberately three fields. A full theme editor is a product in itself, and
/// most of what it would offer is a way to make an accessible palette
/// inaccessible. One brand colour feeds the token layer, which derives its own
/// hover, active and subtle variants and keeps contrast where it must be.
/// </para>
/// </summary>
public class TenantBranding : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>The organisation's own name, in the tenant's primary language.</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>
    /// The name in the other language. Optional: an organisation that operates in
    /// one language should not be made to invent a second name for itself.
    /// </summary>
    public string? DisplayNameAlternate { get; set; }

    /// <summary>Blob name of the logo. Shown in the shell, the exam page and the certificate.</summary>
    public string? LogoBlobName { get; set; }

    /// <summary>A square mark for tight spaces — the browser tab, an email header.</summary>
    public string? IconBlobName { get; set; }

    /// <summary>
    /// The brand colour as #rrggbb. Everything else is derived from it, so a tenant
    /// sets one value rather than a palette they would have to keep consistent.
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Shown on the certificate under the candidate's result. Usually the
    /// organisation's legal name and registration, which the display name is not.
    /// </summary>
    public string? CertificateFooter { get; set; }

    /// <summary>
    /// Where "contact us" points during an exam. A candidate whose connection drops
    /// mid-attempt needs somewhere to go that is not our support address.
    /// </summary>
    public string? SupportEmail { get; set; }

    protected TenantBranding() { }

    public TenantBranding(Guid id, Guid? tenantId, string displayName) : base(id)
    {
        TenantId = tenantId;
        DisplayName = displayName;
    }

    /// <summary>
    /// Accepts a brand colour only in the one form the token layer can consume.
    /// A malformed value would otherwise be injected into a CSS custom property,
    /// where it fails silently and leaves the tenant looking unbranded with no
    /// indication why.
    /// </summary>
    public static bool IsUsableColor(string? value)
    {
        if (value is null || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < 7; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }
}
