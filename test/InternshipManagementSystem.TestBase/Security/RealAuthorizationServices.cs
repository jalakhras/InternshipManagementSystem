using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace InternshipManagementSystem.Security;

/// <summary>
/// Keeps the real authorisation services reachable after
/// <c>AddAlwaysAllowAuthorization()</c> has replaced them.
/// <para>
/// ABP's helper does not add alongside the real registrations, it
/// <see cref="ServiceCollectionDescriptorExtensions.Replace"/>s them: after it runs,
/// the production implementations are not in the collection at all, so a later
/// module cannot get authorisation back simply by deleting the always-allow entries
/// — it would be left with nothing to resolve, and ABP's authorisation interceptor
/// fails to construct.
/// </para>
/// <para>
/// So the collection is copied aside first, into the per-application
/// <c>ServiceConfigurationContext.Items</c> — not a static, because test classes
/// build their applications in parallel and a static would be shared between them.
/// A module that wants authorisation enforced calls <see cref="Restore"/>.
/// </para>
/// <para>
/// Which service types to put back is worked out from the collection rather than
/// listed here. Listing them was tried and was wrong: the helper replaces four
/// registrations, not the three that are obvious, and the fourth
/// (<c>IAbpAuthorizationService</c>, which the method-invocation service actually
/// depends on) is the one that decides every call. Restoring the three obvious ones
/// produced a suite where <c>[Authorize]</c> ran, reached an always-allow
/// implementation underneath, and passed everything — a green permission suite that
/// enforced nothing.
/// </para>
/// </summary>
public static class RealAuthorizationServices
{
    public const string ItemKey = "InternshipManagementSystem.RealAuthorizationServices";

    /// <summary>Call immediately before <c>AddAlwaysAllowAuthorization()</c>.</summary>
    public static void Capture(ServiceConfigurationContext context)
    {
        context.Items[ItemKey] = context.Services.ToList();
    }

    /// <summary>
    /// Put the production implementations back, and take the always-allow ones out.
    /// <para>
    /// Throws rather than degrading if the capture is missing, or if there is
    /// nothing that looks like an always-allow registration to displace. A test
    /// module that silently failed to switch authorisation on would leave every
    /// permission test in the suite green and empty, which is worse than not having
    /// them at all.
    /// </para>
    /// </summary>
    /// <returns>
    /// <c>service → implementation</c> for everything that was put back, so a test
    /// can assert on it by name.
    /// </returns>
    public static IReadOnlyList<string> Restore(ServiceConfigurationContext context)
    {
        if (context.Items.GetOrDefault(ItemKey) is not List<ServiceDescriptor> captured)
        {
            throw new InvalidOperationException(
                $"No service collection was captured under '{ItemKey}'. "
                + "InternshipManagementSystemTestBaseModule must call "
                + "RealAuthorizationServices.Capture(context) immediately before "
                + "AddAlwaysAllowAuthorization().");
        }

        var alwaysAllow = context.Services
            .Where(descriptor => IsAlwaysAllow(descriptor))
            .ToList();

        if (alwaysAllow.Count == 0)
        {
            throw new InvalidOperationException(
                "Nothing registered under a type named AlwaysAllow* was found, so there is "
                + "nothing to displace. Either authorisation is already enforced — in which "
                + "case this module is redundant — or ABP has renamed these types and every "
                + "permission test built on this would be measuring nothing.");
        }

        var displaced = alwaysAllow.Select(descriptor => descriptor.ServiceType).Distinct().ToList();

        var replacements = captured
            .Where(descriptor => displaced.Contains(descriptor.ServiceType))
            .Where(descriptor => !IsAlwaysAllow(descriptor))
            .ToList();

        var unrecoverable = displaced
            .Where(type => replacements.All(descriptor => descriptor.ServiceType != type))
            .Select(type => type.Name)
            .ToList();

        if (unrecoverable.Count > 0)
        {
            throw new InvalidOperationException(
                "These services were replaced by an always-allow implementation and had no "
                + "real registration beforehand to restore: " + string.Join(", ", unrecoverable)
                + ". Enforcement cannot be switched on.");
        }

        foreach (var descriptor in context.Services
                     .Where(descriptor => displaced.Contains(descriptor.ServiceType))
                     .ToList())
        {
            context.Services.Remove(descriptor);
        }

        foreach (var descriptor in replacements)
        {
            context.Services.Add(descriptor);
        }

        return replacements
            .Select(descriptor => descriptor.ServiceType.Name + " → " + ImplementationName(descriptor))
            .Order()
            .ToList();
    }

    private static bool IsAlwaysAllow(ServiceDescriptor descriptor) =>
        descriptor.ImplementationType?.Name.StartsWith("AlwaysAllow", StringComparison.Ordinal) == true;

    private static string ImplementationName(ServiceDescriptor descriptor) =>
        descriptor.ImplementationType?.Name
        ?? descriptor.ImplementationInstance?.GetType().Name
        ?? "(factory)";
}
