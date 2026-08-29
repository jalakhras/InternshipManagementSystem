using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using InternshipManagementSystem.Permissions;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Xunit;

namespace InternshipManagementSystem.Permissions;

/// <summary>
/// That every service is guarded, and that every guard names a policy that exists.
/// <para>
/// These are static checks, and they are here because the dynamic ones cannot
/// exist as this suite is built: the test host calls
/// <c>AddAlwaysAllowAuthorization</c>, so no <c>[Authorize]</c> anywhere in the
/// solution is ever executed by a test. That is a reasonable choice — every
/// integration test would otherwise need a signed-in principal — but it means the
/// authorisation layer has no coverage at all, and this month it let through two
/// defects that reading found and the suite could not:
/// </para>
/// <para>
/// A settings service with no <c>[Authorize]</c> at all. ABP generates a
/// conventional controller for every application service, so it was an anonymous
/// write: anybody could rename the organisation without signing in.
/// </para>
/// <para>
/// And a service whose class-level <c>[Authorize]</c> named a permission the
/// definition provider never defined. ASP.NET answers an undefined policy with a
/// 500 rather than a 403, so a permission mistake presented as a broken screen.
/// </para>
/// <para>
/// Neither needs a running request to detect. Both are properties of the
/// assembly.
/// </para>
/// </summary>
public class AuthorizationCoverageTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(InternshipManagementSystemApplicationModule).Assembly;

    /// <summary>
    /// Services that are anonymous on purpose, and why.
    /// <para>
    /// Named individually rather than pattern-matched, so adding one is a
    /// deliberate act somebody has to write down.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> DeliberatelyAnonymous = new()
    {
        ["ExamTakingAppService"] =
            "The candidate's own path. They have no account and never get one: a link "
            + "is exchanged for a token scoped to a single attempt, and every method "
            + "authorises against that rather than against the staff permission system.",
    };

    [Fact]
    public void Every_application_service_is_guarded()
    {
        var unguarded = ApplicationAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => typeof(IApplicationService).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttributes<AuthorizeAttribute>(inherit: false).ToList().Count == 0)
            .Where(type => !DeliberatelyAnonymous.ContainsKey(type.Name))
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToList();

        // A service with no class-level attribute is an anonymous HTTP endpoint,
        // because ABP registers a conventional controller for the whole assembly.
        // That is how an unauthenticated write to the tenant's settings shipped.
        unguarded.ShouldBeEmpty(
            "these application services have no class-level [Authorize], which makes them "
            + "anonymous endpoints: " + string.Join(", ", unguarded));
    }

    [Fact]
    public async Task Every_policy_named_in_an_attribute_is_defined()
    {
        var defined = await DefinedPermissionsAsync();

        var missing = new List<string>();

        foreach (var type in ApplicationAssembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var policy in PoliciesOn(type))
            {
                if (!defined.Contains(policy))
                {
                    missing.Add($"{type.Name} (class) → {policy}");
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var policy in PoliciesOn(method))
                {
                    if (!defined.Contains(policy))
                    {
                        missing.Add($"{type.Name}.{method.Name} → {policy}");
                    }
                }
            }
        }

        // ASP.NET answers an undefined policy with a 500, not a 403 — so this
        // mistake reads as a broken screen rather than as a permission problem,
        // and nobody looks at the permission tree. It shipped once already:
        // Users.Default was authorised against and never defined, and every call
        // to the user list returned 500.
        missing.ShouldBeEmpty(
            "these [Authorize] attributes name policies the permission definition provider "
            + "does not define, which produces a 500 rather than a 403: "
            + string.Join("; ", missing));
    }

    [Fact]
    public async Task Every_defined_permission_is_enforced_somewhere()
    {
        var defined = await DefinedPermissionsAsync();
        var used = new HashSet<string>();

        foreach (var type in ApplicationAssembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            used.UnionWith(PoliciesOn(type));

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                used.UnionWith(PoliciesOn(method));
            }
        }

        // Checked in code as well as in attributes: some permissions are enforced
        // by an explicit call, because the decision depends on what changed.
        var source = System.IO.File.ReadAllText(SourceOf("InternshipManagementSystemPermissions.cs"));

        var unenforced = defined
            .Where(permission => !used.Contains(permission))

            // A parent in the tree is a grouping, not a guard. Granting a child
            // grants it, and no service authorises against it directly.
            .Where(permission => !IsGroupingPermission(permission, defined))

            .Where(permission => !EnforcedInCode(permission))
            .OrderBy(name => name)
            .ToList();

        // A grantable permission that enforces nothing is a promise in the
        // administration screen that the product does not keep. Four of them were
        // found this month — Attempts.View, .ForceSubmit, .Delete and
        // Users.ManageRoles — and the first three had no implementation at all
        // while the fourth let anyone who could edit a colleague make themselves
        // an administrator.
        unenforced.ShouldBeEmpty(
            "these permissions can be granted and guard nothing: " + string.Join(", ", unenforced)
            + ". Either enforce them or remove them — a permission that does nothing is a "
            + "promise the administration screen makes and the product does not keep.");
    }

    // ------------------------------------------------------------------ helpers

    private static IEnumerable<string> PoliciesOn(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Select(policy => policy!);

    /// <summary>Every permission this application defines, read from the provider itself.</summary>
    private static async Task<HashSet<string>> DefinedPermissionsAsync()
    {
        var context = new PermissionDefinitionContext(null!);

        new InternshipManagementSystemPermissionDefinitionProvider().Define(context);

        await Task.CompletedTask;

        return context.Groups.Values
            .SelectMany(group => group.GetPermissionsWithChildren())
            .Select(permission => permission.Name)
            .ToHashSet();
    }

    /// <summary>
    /// Whether this permission exists only to hold others.
    /// <para>
    /// A parent whose children are the real guards. ABP's permission screen grants
    /// a parent along with a child, and no service authorises against it, so an
    /// unused parent is by design rather than an omission.
    /// </para>
    /// </summary>
    private static bool IsGroupingPermission(string permission, HashSet<string> all) =>
        all.Any(other => other != permission && other.StartsWith(permission + ".", StringComparison.Ordinal));

    /// <summary>
    /// Whether a permission is enforced by an explicit check rather than an attribute.
    /// <para>
    /// Some decisions depend on what the caller is actually changing — sending
    /// email, or altering a role list — and cannot be expressed as an attribute on
    /// the method.
    /// </para>
    /// </summary>
    private static bool EnforcedInCode(string permission)
    {
        var leaf = permission.Split('.').Last();

        return ApplicationSources.Value.Any(source => source.Contains($".{leaf});") || source.Contains($".{leaf})"));
    }

    private static readonly Lazy<List<string>> ApplicationSources = new(() =>
    {
        var root = SourceRoot();

        return System.IO.Directory
            .EnumerateFiles(
                System.IO.Path.Combine(root, "src", "InternshipManagementSystem.Application"),
                "*.cs",
                System.IO.SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}")
                           && !path.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}"))
            .Select(System.IO.File.ReadAllText)
            .ToList();
    });

    private static string SourceOf(string fileName) =>
        System.IO.Directory
            .EnumerateFiles(SourceRoot(), fileName, System.IO.SearchOption.AllDirectories)
            .First();

    /// <summary>
    /// The repository root, walked up from wherever the test binary sits.
    /// </summary>
    private static string SourceRoot()
    {
        var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new InvalidOperationException("Could not find the repository root from " + AppContext.BaseDirectory);
    }
}
