using System;
using System.Collections.Generic;
using System.Linq;

namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// The set of permissions the signed-in person holds, for the duration of one test.
/// <para>
/// This exists because the rest of the suite runs under
/// <c>AddAlwaysAllowAuthorization</c>, where no <c>[Authorize]</c> attribute in the
/// solution is ever executed. Tests deriving from
/// <see cref="PermissionEnforcedTestBase"/> run with that switch removed and this
/// object standing in for the grant store, so a permission is held only if a test
/// says so.
/// </para>
/// <para>
/// Granting is literal: holding <c>Assessment.Review.Grade</c> does not imply
/// holding <c>Assessment.Review</c>. That is how ABP stores grants — one row per
/// permission name — and a service guarded at both class and method level is
/// therefore refused to somebody who holds only one of the two. Tests that want
/// the whole chain must name every link, which is the point: it makes the chain
/// visible.
/// </para>
/// </summary>
public class GrantedPermissions
{
    private readonly HashSet<string> _granted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _denied = new(StringComparer.Ordinal);

    private bool _everything;

    /// <summary>Hold every permission. For building a fixture, not for asserting on.</summary>
    public void GrantEverything()
    {
        _everything = true;
        _granted.Clear();
        _denied.Clear();
    }

    /// <summary>
    /// Hold every permission except the named ones.
    /// <para>
    /// The strongest shape for a refusal test: it says the guard, and only the
    /// guard, is what stands between this person and the operation. A test that
    /// grants nothing at all cannot tell a working guard from a service that
    /// fails for some unrelated reason.
    /// </para>
    /// </summary>
    public void GrantEverythingExcept(params string[] permissions)
    {
        if (permissions.Length == 0)
        {
            throw new ArgumentException(
                "GrantEverythingExcept() with nothing denied is GrantEverything(), and a "
                + "refusal test written that way asserts nothing.",
                nameof(permissions));
        }

        _everything = true;
        _granted.Clear();
        _denied.Clear();
        _denied.UnionWith(permissions);
    }

    /// <summary>Hold exactly these permissions and nothing else.</summary>
    public void GrantOnly(params string[] permissions)
    {
        _everything = false;
        _granted.Clear();
        _denied.Clear();
        _granted.UnionWith(permissions);
    }

    public bool IsGranted(string name)
    {
        if (_denied.Contains(name))
        {
            return false;
        }

        return _everything || _granted.Contains(name);
    }

    public override string ToString() =>
        _everything
            ? "everything" + (_denied.Count > 0 ? " except " + string.Join(", ", _denied.Order()) : string.Empty)
            : _granted.Count == 0 ? "nothing" : string.Join(", ", _granted.Order());
}
