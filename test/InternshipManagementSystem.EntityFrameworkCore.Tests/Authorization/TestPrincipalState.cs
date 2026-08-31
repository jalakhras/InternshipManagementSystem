namespace InternshipManagementSystem.EntityFrameworkCore.Authorization;

/// <summary>
/// Whether the caller is signed in at all, for the duration of one test.
/// <para>
/// Separate from <see cref="GrantedPermissions"/> because they answer different
/// questions, and one of them was silently unanswerable. A bare <c>[Authorize]</c>
/// carrying no policy resolves to ASP.NET's default policy —
/// <c>RequireAuthenticatedUser()</c> — which a signed-in principal satisfies no
/// matter how few permissions it holds. So a test that only varied the permissions
/// could not tell that somebody had added <c>[Authorize]</c> to the candidate's
/// path, which is the single most damaging attribute anybody could add in this
/// product: it would stop every candidate from sitting an exam.
/// </para>
/// <para>
/// Proven, not assumed: with the tests written that way, adding <c>[Authorize]</c>
/// to <c>ExamTakingAppService</c> in a scratchpad copy left them green.
/// </para>
/// </summary>
public class TestPrincipalState
{
    public bool SignedIn { get; private set; } = true;

    public void SignIn() => SignedIn = true;

    /// <summary>Nobody at all — which is what a candidate holding a link is.</summary>
    public void SignOut() => SignedIn = false;
}
