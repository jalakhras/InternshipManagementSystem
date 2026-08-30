using Volo.Abp.Account.Settings;
using Volo.Abp.Settings;

namespace InternshipManagementSystem;

/// <summary>
/// Closes the registration page this product never asked for.
/// <para>
/// ABP's account module ships a working self-registration page and turns it on
/// by default, so <c>/Account/Register</c> answered 200 with a live form: anybody
/// who found the address could create an account inside a customer's
/// organisation. Nothing here wants that. Candidates never have accounts — a
/// link is their entire credential — and a staff account is something an
/// administrator creates on the Users screen, deliberately, with a role
/// attached, because an account with no role can sign in and see an empty
/// application.
/// </para>
/// <para>
/// There was a switch called "let people create their own accounts" on the
/// settings screen, and it was this product's own setting, wired to nothing,
/// sitting beside a registration page it did not control. An administrator could
/// turn it off, watch it save, and still be accepting registrations. That switch
/// is gone; this is the real one, and it is off.
/// </para>
/// <para>
/// It lives in the host, which is the only project that loads ABP's account
/// <i>web</i> module — and that is where the definition being overridden comes
/// from. Putting it in Application.Contracts, which merely depends on the
/// account <i>contracts</i>, silently did nothing: a provider can only override
/// a definition that already exists, <c>GetOrNull</c> returned null, and the
/// page stayed open with no error anywhere. Seeding the value instead does not
/// work either — the DbMigrator has no account module, so the setting is not
/// defined in its world at all and setting it throws.
/// </para>
/// <para>
/// A default rather than a stored value, so an organisation that genuinely wants
/// open registration can still turn it on and keep it through a deployment.
/// </para>
/// </summary>
public class AccountSettingOverrides : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        var selfRegistration = context.GetOrNull(AccountSettingNames.IsSelfRegistrationEnabled);

        if (selfRegistration is not null)
        {
            selfRegistration.DefaultValue = "false";
        }
    }
}
