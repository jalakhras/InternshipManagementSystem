using System;
using System.Threading.Tasks;
using Volo.Abp.Authorization;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// Base for tests that need <c>[Authorize]</c> to actually run.
/// <para>
/// Build the fixture under <see cref="GrantEverything"/> — creating an exam and
/// sitting it needs a dozen permissions that are not the subject of the test — then
/// narrow the grant to the one thing under examination and call the method.
/// </para>
/// </summary>
public abstract class PermissionEnforcedTestBase : InternshipManagementSystemTestBase<PermissionEnforcedTestModule>
{
    protected GrantedPermissions Permissions => GetRequiredService<GrantedPermissions>();

    protected void GrantEverything() => Permissions.GrantEverything();

    protected void GrantEverythingExcept(params string[] permissions) =>
        Permissions.GrantEverythingExcept(permissions);

    protected void GrantOnly(params string[] permissions) => Permissions.GrantOnly(permissions);

    /// <summary>Nobody at all: no account, no claims, no permissions.</summary>
    protected void SignOutCompletely()
    {
        GetRequiredService<TestPrincipalState>().SignOut();
        Permissions.GrantOnly();
    }

    protected void SignInAsStaff() => GetRequiredService<TestPrincipalState>().SignIn();

    /// <summary>
    /// That the call is refused, and refused as an authorisation decision rather than
    /// by anything else going wrong.
    /// <para>
    /// <c>Should.ThrowAsync&lt;AbpAuthorizationException&gt;</c> on its own is weaker
    /// than it looks — but weaker still is asserting only that something threw, which
    /// a typo in a fixture satisfies. The paired "and it succeeds when granted" test
    /// beside every use of this is what stops it passing for the wrong reason.
    /// </para>
    /// </summary>
    protected static async Task<AbpAuthorizationException> RefusedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (AbpAuthorizationException exception)
        {
            return exception;
        }

        throw new Xunit.Sdk.XunitException(
            "The call was allowed. Expected AbpAuthorizationException, which means the "
            + "[Authorize] attribute this test names is missing, or is not being executed.");
    }
}
