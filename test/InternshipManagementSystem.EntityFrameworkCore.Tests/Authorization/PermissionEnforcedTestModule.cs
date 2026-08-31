using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Security.Claims;
using Volo.Abp.Modularity;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// The same application as the rest of the suite, with authorisation switched back on.
/// <para>
/// <c>InternshipManagementSystemTestBaseModule</c> calls
/// <c>AddAlwaysAllowAuthorization()</c>, which registers three implementations that
/// answer yes to everything: <c>AlwaysAllowAuthorizationService</c>,
/// <c>AlwaysAllowMethodInvocationAuthorizationService</c> and
/// <c>AlwaysAllowPermissionChecker</c>. It is registered in the shared base module,
/// so every backend test in the repository inherits it and not one
/// <c>[Authorize]</c> attribute in the solution has ever been executed by a test.
/// </para>
/// <para>
/// This module depends on the ordinary EF Core test module, so its
/// <c>ConfigureServices</c> runs last, and it puts the production implementations
/// back — the ones the base module copied aside before the always-allow helper
/// replaced them. ABP's real pipeline then runs intact: the Castle interceptor reads
/// <c>[Authorize]</c>, <c>MethodInvocationAuthorizationService</c> turns it into a
/// policy check, ASP.NET's policy provider resolves the permission, and ABP's
/// requirement handler asks the permission checker. Only that last step is a test
/// double — <see cref="TestPermissionChecker"/> — so a test can say what the caller
/// holds without seeding a role, a user and a grant row for every case.
/// </para>
/// <para>
/// <see cref="Security.RealAuthorizationServices.Restore"/> throws rather than
/// degrading if there is nothing to restore. An ABP upgrade that changed those
/// registrations would otherwise leave every test in this namespace green and
/// testing nothing, which is the exact failure this exercise exists to remove.
/// </para>
/// </summary>
[DependsOn(typeof(InternshipManagementSystemEntityFrameworkCoreTestModule))]
public class PermissionEnforcedTestModule : AbpModule
{
    /// <summary>What was put back, so a test can assert the swap actually happened.</summary>
    public static IReadOnlyList<string> RestoredAuthorizationServices { get; private set; } = [];

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        RestoredAuthorizationServices = Security.RealAuthorizationServices.Restore(context);

        // Signed in, which the shared fake principal is not. See
        // AuthenticatedTestPrincipalAccessor for why that only becomes visible
        // once [Authorize] is actually executed.
        foreach (var descriptor in context.Services
                     .Where(descriptor => descriptor.ServiceType == typeof(ICurrentPrincipalAccessor))
                     .ToList())
        {
            context.Services.Remove(descriptor);
        }

        context.Services.AddSingleton<ICurrentPrincipalAccessor, AuthenticatedTestPrincipalAccessor>();

        // One object per application, and each test class gets its own application,
        // so grants set by one test cannot reach another.
        context.Services.AddSingleton<GrantedPermissions>();
        context.Services.AddSingleton<TestPrincipalState>();
        context.Services.AddSingleton<IPermissionChecker, TestPermissionChecker>();
    }
}
