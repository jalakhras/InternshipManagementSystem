using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.Catalog;
using InternshipManagementSystem.Assessment.Catalog.Dtos;
using InternshipManagementSystem.Permissions;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// That the harness itself is real, before anything is concluded from it.
/// <para>
/// Every other test in this namespace rests on two facts: the always-allow
/// registrations are gone, and the permission checker being consulted is the one the
/// test sets. If either stopped being true — an ABP rename, a stray registration
/// ordering change — the refusal tests would go green, permanently, while proving
/// nothing. So both are asserted directly.
/// </para>
/// </summary>
public class PermissionEnforcementHarnessTests : PermissionEnforcedTestBase
{
    [Fact]
    public void The_real_authorisation_services_are_in_use()
    {
        // Named individually rather than counted: a change should read as "this
        // one is missing", not as "the number changed". IAbpAuthorizationService is
        // the one to watch — it is what MethodInvocationAuthorizationService
        // actually calls, and leaving it always-allow gives a permission suite that
        // executes every [Authorize] and still passes everything.
        PermissionEnforcedTestModule.RestoredAuthorizationServices.ShouldBe(
        [
            "IAbpAuthorizationService → AbpAuthorizationService",
            "IAuthorizationService → AbpAuthorizationService",
            "IMethodInvocationAuthorizationService → MethodInvocationAuthorizationService",
            "IPermissionChecker → PermissionChecker",
        ]);
    }

    [Fact]
    public void The_permission_checker_in_use_is_the_one_a_test_can_set()
    {
        GetRequiredService<IPermissionChecker>().ShouldBeOfType<TestPermissionChecker>();
    }

    [Fact]
    public async Task A_class_level_guard_refuses_and_allows_the_same_call()
    {
        // Refused: everything except the one permission the class names.
        GrantEverythingExcept(InternshipManagementSystemPermissions.Catalog.View);

        var catalog = GetRequiredService<ICatalogAppService>();

        await RefusedAsync(() => catalog.GetCategoriesAsync());

        // And allowed with it — the half that stops the refusal above passing
        // because the service is broken for some unrelated reason.
        GrantOnly(InternshipManagementSystemPermissions.Catalog.View);

        var categories = await catalog.GetCategoriesAsync();

        categories.ShouldNotBeNull();
    }
}
