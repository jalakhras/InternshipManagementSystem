using InternshipManagementSystem.Assessment.Delivery;
using MailKit.Security;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment;

/// <summary>
/// The one line that decides whether an invitation leaves the building.
/// <para>
/// Found by sending a real message through a real relay and reading a server
/// log — which is not a way to find it a second time. A deployment configured
/// exactly as Google, Brevo and Microsoft all document it, port 587 with TLS,
/// failed its handshake and sent nothing; the send is caught so that a bulk
/// assignment does not die half way, so the screen reported every link as
/// issued and said nothing at all.
/// </para>
/// <para>
/// And an invitation that does not arrive is a candidate who cannot sit. The
/// link inside it is their entire credential: no account, no password, nothing
/// else to try.
/// </para>
/// </summary>
public class MailTransportTests
{
    [Fact]
    public void Port_587_upgrades_the_connection_rather_than_starting_encrypted()
    {
        // The port every provider's documentation names. It expects a plain
        // connection that STARTTLS upgrades; opening SSL on it hangs until the
        // socket times out.
        MailTransport.SecurityFor(587, enableSsl: true)
            .ShouldBe(SecureSocketOptions.StartTls);
    }

    [Fact]
    public void Port_587_is_encrypted_even_when_nobody_asked()
    {
        // A deployment that names 587 and forgets the flag has not asked to send
        // its password across the internet in the clear. It has made a mistake
        // the port itself answers.
        MailTransport.SecurityFor(587, enableSsl: false)
            .ShouldBe(SecureSocketOptions.StartTls);
    }

    [Fact]
    public void Port_465_is_encrypted_from_the_first_byte()
    {
        // The older convention, and still the only one some networks let out.
        MailTransport.SecurityFor(465, enableSsl: true)
            .ShouldBe(SecureSocketOptions.SslOnConnect);
    }

    [Fact]
    public void A_relay_that_asks_for_encryption_on_another_port_gets_it()
    {
        MailTransport.SecurityFor(2525, enableSsl: true)
            .ShouldBe(SecureSocketOptions.StartTls);
    }

    [Fact]
    public void A_plain_relay_takes_encryption_where_it_is_offered_and_works_without()
    {
        // A catcher on a developer's machine, or a relay inside a private
        // network, has no certificate and needs none. Refusing to speak without
        // one would make the product undeliverable in exactly the places it is
        // easiest to deliver.
        MailTransport.SecurityFor(2525, enableSsl: false)
            .ShouldBe(SecureSocketOptions.StartTlsWhenAvailable);
    }
}
