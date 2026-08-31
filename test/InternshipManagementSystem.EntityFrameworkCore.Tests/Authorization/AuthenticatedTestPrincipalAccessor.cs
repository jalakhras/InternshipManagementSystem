using System.Collections.Generic;
using System.Security.Claims;
using Volo.Abp.Security.Claims;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// The same fake staff member as the rest of the suite, but signed in.
/// <para>
/// <c>FakeCurrentPrincipalAccessor</c> builds its identity as
/// <c>new ClaimsIdentity(claims)</c>, with no authentication type. .NET reads that
/// as <c>IsAuthenticated == false</c>, which never mattered while every
/// <c>[Authorize]</c> was answered by an always-allow implementation. With
/// authorisation switched on it matters immediately: a bare <c>[Authorize]</c>
/// carrying no policy — <c>CandidateAppService</c> has one — resolves to ASP.NET's
/// default policy, which is <c>RequireAuthenticatedUser()</c>, and refuses that
/// principal no matter what permissions it holds.
/// </para>
/// <para>
/// Naming an authentication type is what a real sign-in does, so this is the
/// production-shaped principal, not a loosening. It is registered only for
/// <see cref="PermissionEnforcedTestModule"/>; the rest of the suite is untouched.
/// </para>
/// </summary>
public class AuthenticatedTestPrincipalAccessor : ThreadCurrentPrincipalAccessor
{
    public const string UserId = "2e701e62-0953-4dd3-910b-dc6cc93ccb0d";

    private readonly TestPrincipalState _state;

    public AuthenticatedTestPrincipalAccessor(TestPrincipalState state)
    {
        _state = state;
    }

    protected override ClaimsPrincipal GetClaimsPrincipal()
    {
        if (!_state.SignedIn)
        {
            // No identity of any kind. A candidate has no account and never gets
            // one, and this is the only principal shape that can tell an
            // anonymous-by-design service from one that quietly grew a guard.
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
            new List<Claim>
            {
                new(AbpClaimTypes.UserId, UserId),
                new(AbpClaimTypes.UserName, "admin"),
                new(AbpClaimTypes.Email, "admin@abp.io"),
            },
            authenticationType: "Test"));
    }
}
