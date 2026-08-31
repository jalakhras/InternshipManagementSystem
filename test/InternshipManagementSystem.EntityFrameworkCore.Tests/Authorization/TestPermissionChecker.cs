using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// Stands in for ABP's grant store, and for nothing else.
/// <para>
/// Everything above this — the Castle interceptor that reads <c>[Authorize]</c>,
/// <c>MethodInvocationAuthorizationService</c>, ASP.NET's policy provider, ABP's
/// <c>PermissionRequirementHandler</c> — is the real production pipeline. Only the
/// question "does this person hold this permission" is answered from a test fixture
/// rather than from <c>AbpPermissionGrants</c>, because seeding a role, a user, a
/// grant row and a signed-in principal for every test would buy nothing: the row is
/// not the behaviour under test, the attribute is.
/// </para>
/// </summary>
public class TestPermissionChecker : IPermissionChecker
{
    private readonly GrantedPermissions _granted;

    public TestPermissionChecker(GrantedPermissions granted)
    {
        _granted = granted;
    }

    public Task<bool> IsGrantedAsync(string name) =>
        Task.FromResult(_granted.IsGranted(name));

    public Task<bool> IsGrantedAsync(ClaimsPrincipal? claimsPrincipal, string name) =>
        Task.FromResult(_granted.IsGranted(name));

    public Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names) =>
        IsGrantedAsync(null, names);

    public Task<MultiplePermissionGrantResult> IsGrantedAsync(ClaimsPrincipal? claimsPrincipal, string[] names)
    {
        var result = new MultiplePermissionGrantResult();

        foreach (var name in names.Distinct())
        {
            result.Result[name] = _granted.IsGranted(name)
                ? PermissionGrantResult.Granted
                : PermissionGrantResult.Prohibited;
        }

        return Task.FromResult(result);
    }
}
