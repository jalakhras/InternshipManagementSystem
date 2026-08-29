using System;
using System.Net;
using System.Text.RegularExpressions;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// The message a candidate actually receives.
/// <para>
/// A pure function of what goes in it, separate from sending, because this is
/// the only part of the product that reaches somebody who has no account, no
/// relationship with us and no reason to trust the link — and it needs to be
/// assertable without a mail server.
/// </para>
/// <para>
/// Sent in the organisation's name. It carried none: a candidate received "you
/// have been assigned an assessment" from nobody, pointing at a domain they had
/// never seen, which is the exact shape of the message people are taught not to
/// open. The name was in the tenant's settings, written on the settings screen,
/// and read by nothing a candidate ever saw.
/// </para>
/// <para>
/// The logo is deliberately not embedded. The blob sits behind a signed media
/// grant and a mail client has no token, so it would arrive as a broken image —
/// worse for trust than no image. A name and a colour need no credential.
/// </para>
/// </summary>
public static class InvitationEmail
{
    /// <summary>Used when a tenant has set no colour, or set one that is not a colour.</summary>
    public const string DefaultBrandColor = "#0f6c8c";

    private static readonly Regex HexColor =
        new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    public sealed record Message(string Subject, string Body);

    public static Message Build(
        string? organizationName,
        string? brandColor,
        string candidateName,
        string examTitle,
        int minutes,
        DateTime expiresAt,
        string url)
    {
        // A tenant that has not named itself gets a sentence that reads correctly
        // without a name, rather than a placeholder standing in for one.
        var named = !string.IsNullOrWhiteSpace(organizationName);
        var org = Escape(organizationName?.Trim() ?? string.Empty);

        var name = Escape(candidateName);
        var title = Escape(examTitle);

        // Every one of these is tenant- or coordinator-supplied text on its way
        // into HTML in somebody else's inbox. A candidate named with a stray
        // angle bracket would otherwise rewrite the mail around themselves.
        var href = Escape(url);

        var brand = HexColor.IsMatch(brandColor ?? string.Empty)
            ? brandColor!
            : DefaultBrandColor;

        var subject = named
            ? $"{organizationName!.Trim()} — {examTitle} — دعوة لأداء اختبار / Assessment invitation"
            : $"{examTitle} — دعوة لأداء اختبار / Assessment invitation";

        var button =
            "display:inline-block;padding:.6em 1.4em;border-radius:6px;" +
            $"background:{brand};color:#fff;text-decoration:none;font-weight:600";

        var body = $"""
            <div dir="rtl" style="font-family:system-ui,sans-serif;line-height:1.7">
              <p>مرحباً {name},</p>
              <p>{(named ? $"أسندت إليك <strong>{org}</strong> اختبار" : "لقد تم إسناد اختبار")} <strong>{title}</strong>.</p>
              <ul>
                <li>المدة: {minutes} دقيقة</li>
                <li>صلاحية الرابط حتى: {expiresAt:yyyy-MM-dd HH:mm}</li>
              </ul>
              <p><a href="{href}" style="{button}">ابدأ الاختبار</a></p>
              <p style="color:#666;font-size:.9em">لا يبدأ العدّ التنازلي إلا عند ضغطك على زر البدء.</p>
            </div>
            <hr>
            <div dir="ltr" style="font-family:system-ui,sans-serif;line-height:1.7">
              <p>Hello {name},</p>
              <p>{(named ? $"<strong>{org}</strong> has assigned you" : "You have been assigned")} <strong>{title}</strong>.</p>
              <ul>
                <li>Duration: {minutes} minutes</li>
                <li>Link valid until: {expiresAt:yyyy-MM-dd HH:mm}</li>
              </ul>
              <p><a href="{href}" style="{button}">Start the assessment</a></p>
              <p style="color:#666;font-size:.9em">The timer does not start until you press start.</p>
            </div>
            """;

        return new Message(subject, body);
    }

    private static string Escape(string value) => WebUtility.HtmlEncode(value);
}
