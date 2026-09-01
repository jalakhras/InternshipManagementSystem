using System.Linq;
using System.Reflection;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using Shouldly;
using Volo.Abp.Auditing;
using Xunit;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// What the audit log is allowed to keep about a candidate.
/// <para>
/// Two things were reaching <c>AbpAuditLogActions.Parameters</c> that had no
/// business being there, and neither was noticed by any test in this repository
/// because no test in this repository looks at the audit log.
/// </para>
/// <para>
/// <b>The session token.</b> This product states plainly that a candidate has no
/// account and that the token is the whole of their credential. It was being
/// written into an audit row in plain text, and it stays valid until the sitting
/// ends. Anybody who could read that table could have sat the exam as them for
/// as long as it ran.
/// </para>
/// <para>
/// <b>The answer.</b> Every save wrote a second copy of the response, and the
/// copy was outside everything the product does to keep its word about the
/// first: deleting an organisation clears nineteen assessment tables and the
/// files beside them, and does not touch the audit log.
/// </para>
/// <para>
/// The two need different tools and that is the point of testing both. A value
/// arriving as a method parameter can only be dropped by not auditing the
/// method; a value arriving as a property of a DTO is dropped by marking the
/// property, and marking the class that receives it was measured and changed
/// nothing.
/// </para>
/// </summary>
public class AuditedAnswerTests
{
    [Fact]
    public void A_candidates_answer_is_not_kept_in_the_audit_log()
    {
        var response = typeof(SaveAnswerDto).GetProperty(nameof(SaveAnswerDto.Response));

        response.ShouldNotBeNull();

        // Asked of the property because that is where ABP looks. The same
        // attribute on the controller was tried first, and the answer stayed in
        // the row — reading the rows is what found that out, counting them said
        // the fix had worked.
        response!.GetCustomAttributes<DisableAuditingAttribute>(inherit: true)
                 .ShouldNotBeEmpty();
    }

    [Fact]
    public void The_service_that_is_handed_a_candidates_credential_is_not_audited()
    {
        var service = typeof(ExamTakingAppService);

        // Every public method here takes the session token as a parameter, and a
        // parameter cannot be marked. Not auditing the service is the only tool
        // that reaches it.
        service.GetCustomAttributes<DisableAuditingAttribute>(inherit: true)
               .ShouldNotBeEmpty();
    }

    [Fact]
    public void Every_method_holding_the_credential_is_covered_by_that()
    {
        var methods = typeof(ExamTakingAppService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetParameters().Any(p => p.Name is "sessionToken" or "token"))
            .ToList();

        // The reason the class-level attribute is the right shape rather than a
        // blunt one: this is not one method with a token, it is most of them.
        // A future method added here is covered on the day it is written.
        methods.Count.ShouldBeGreaterThan(3);
    }
}
