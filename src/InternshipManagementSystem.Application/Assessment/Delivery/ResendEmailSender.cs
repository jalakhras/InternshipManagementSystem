using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Sending the invitation over HTTPS instead of SMTP.
/// <para>
/// Not a preference. The network this was first configured on accepts a TCP
/// connection to smtp.gmail.com on 587 and on 465 and then never answers —
/// the connection looks established and no SMTP greeting ever arrives. Port 25
/// is refused outright. Port 443 works.
/// </para>
/// <para>
/// That is not one unlucky machine. Blocking outbound SMTP is ordinary practice
/// for consumer and corporate networks in the region this product is built for,
/// and a customer who cannot send invitations cannot use the product at all —
/// the link inside that message is the candidate's entire credential.
/// </para>
/// <para>
/// So the transport is a choice, made by which credential is configured, and
/// the message itself is untouched: the same Arabic-then-English body, the same
/// organisation name and colour, the same link. Only the road it travels
/// changes.
/// </para>
/// </summary>
/// <remarks>
/// Registered by hand, never by convention.
/// <para>
/// ABP's <c>EmailSenderBase</c> exposes itself as <c>IEmailSender</c>, so a
/// subclass discovered conventionally becomes <i>the</i> mail sender everywhere
/// — including in the test host, which has no HTTP client factory and no key,
/// and where ten tests that never mention mail failed on a constructor they
/// could not build. It is registered only where its key exists, in
/// <c>ConfigureEmailTransport</c>.
/// </para>
/// </remarks>
[DisableConventionalRegistration]
public class ResendEmailSender : EmailSenderBase
{
    /// <summary>Resend's send endpoint. One POST, one message.</summary>
    private const string Endpoint = "https://api.resend.com/emails";

    private readonly IHttpClientFactory _clients;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        ICurrentTenant currentTenant,
        IEmailSenderConfiguration configuration,
        IBackgroundJobManager backgroundJobManager,
        IHttpClientFactory clients,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailSender> logger)
        : base(currentTenant, configuration, backgroundJobManager)
    {
        _clients = clients;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task SendEmailAsync(MailMessage mail)
    {
        var client = _clients.CreateClient(nameof(ResendEmailSender));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            from = From(mail),
            to = new[] { mail.To[0].Address },
            subject = mail.Subject,
            html = mail.Body,
        };

        var response = await client.PostAsJsonAsync(Endpoint, payload);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // The body says why, and the reason is almost always one of two things
        // worth reading rather than guessing at: an unverified sending domain,
        // or a recipient the account is not yet allowed to write to. Both are
        // configuration, and neither is visible from a status code.
        var reason = await response.Content.ReadAsStringAsync();

        _logger.LogError(
            "Resend refused the invitation to {To}: {Status} {Reason}",
            mail.To[0].Address,
            (int)response.StatusCode,
            reason);

        throw new AbpException(
            $"Resend refused the message ({(int)response.StatusCode}): {reason}");
    }

    /// <summary>
    /// Who it comes from.
    /// <para>
    /// The configured sender wins, because a verified domain is the thing that
    /// keeps an invitation out of a spam folder — and an invitation in a spam
    /// folder is a candidate who does not sit. Falling back to whatever the
    /// message carried would silently send from an address the account has not
    /// proved it owns, which Resend refuses and which no log would explain.
    /// </para>
    /// </summary>
    private string From(MailMessage mail) =>
        !string.IsNullOrWhiteSpace(_options.From)
            ? _options.From!
            : mail.From?.ToString() ?? throw new AbpException(
                "Mailing:Resend:From is not set and the message carries no sender.");
}

/// <summary>How to reach Resend, and who mail comes from.</summary>
public class ResendOptions
{
    /// <summary>
    /// The API key. Read from configuration, and belongs in user secrets or an
    /// environment variable — never in a file that git tracks.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The sender, as a display name and address: <c>Astrolabe &lt;no-reply@…&gt;</c>.
    /// <para>
    /// Until a domain is verified this has to be Resend's own test sender, and
    /// mail can only reach the account holder. That limit is theirs and it is
    /// the right one: an unverified sender is how a phishing message is sent.
    /// </para>
    /// </summary>
    public string? From { get; set; }
}
