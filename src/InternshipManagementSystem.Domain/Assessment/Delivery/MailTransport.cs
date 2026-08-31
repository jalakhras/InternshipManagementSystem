using MailKit.Security;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// How the connection to a mail relay is secured.
/// <para>
/// The default treats "use SSL" as SSL from the first byte. That is right on
/// port 465 and wrong on 587 — and 587 is the port every provider documents,
/// because it expects a plain connection upgraded by STARTTLS.
/// </para>
/// <para>
/// Configured the way Google, Brevo and Microsoft all say to configure it, the
/// handshake failed and <b>no invitation was sent</b>. The whole failure was one
/// line in a server log: the send is caught so a bulk assignment does not die
/// half way, so the screen reported the links as issued and said nothing.
/// </para>
/// <para>
/// That is the worst shape this can take. The link inside the message is the
/// candidate's entire credential — there is no account and no password to fall
/// back on — so an invitation that does not arrive is a candidate who cannot
/// sit, and the coordinator who sent it has no way to find out.
/// </para>
/// <para>
/// Derived from the port rather than asked for as a fourth setting. Somebody
/// configuring mail already has to get the host, the port and the credentials
/// right; making them also name a socket option is one more thing to be wrong
/// about that the port already answers.
/// </para>
/// </summary>
public static class MailTransport
{
    /// <param name="port">The relay's port.</param>
    /// <param name="enableSsl">Whether the deployment asked for an encrypted connection.</param>
    public static SecureSocketOptions SecurityFor(int port, bool enableSsl) => (port, enableSsl) switch
    {
        // Implicit TLS: encrypted before a byte of SMTP is spoken. The older
        // convention, and still the only one some networks let out.
        (465, _) => SecureSocketOptions.SslOnConnect,

        // Submission. A plain connection the client upgrades with STARTTLS,
        // which is what every provider's documentation means by "TLS" here.
        //
        // Note this ignores the flag. A deployment that names 587 and forgets to
        // set EnableSsl has not asked for an unencrypted connection to a public
        // relay — it has made a mistake that would send the credential in the
        // clear, and the port says plainly what was meant.
        (587, _) => SecureSocketOptions.StartTls,

        // Anything else: honour what was asked for, and take encryption where it
        // is offered rather than refusing to speak without it. A relay on 25
        // inside a private network has no certificate and needs none.
        (_, true) => SecureSocketOptions.StartTls,
        (_, false) => SecureSocketOptions.StartTlsWhenAvailable,
    };
}
