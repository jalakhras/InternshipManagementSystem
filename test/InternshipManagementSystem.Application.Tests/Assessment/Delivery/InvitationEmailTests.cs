using System;
using InternshipManagementSystem.Assessment.Delivery;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// The one message that reaches somebody with no account and no reason to trust
/// it.
/// <para>
/// A candidate opening a placement-test link has never heard of us. Everything
/// that makes the mail credible — whose name is on it, where the link goes —
/// has to be right, and everything that goes into it comes from somewhere a
/// person typed: a tenant's settings, a coordinator's spreadsheet.
/// </para>
/// </summary>
public class InvitationEmailTests
{
    private static readonly DateTime Expires = new(2026, 9, 4, 17, 0, 0);

    private const string Url = "https://exams.example.test/exam/abc123";

    [Fact]
    public void The_organisation_signs_it()
    {
        var message = Build(organizationName: "أكاديمية التداول");

        // In the subject line, which is the part read in a notification before
        // anything is opened.
        message.Subject.ShouldStartWith("أكاديمية التداول — ");

        // And in both languages of the body, because the recipient may read
        // either and guessing wrong on a one-shot invitation is expensive.
        message.Body.ShouldContain("أسندت إليك <strong>أكاديمية التداول</strong>");
        message.Body.ShouldContain("<strong>أكاديمية التداول</strong> has assigned you");
    }

    [Fact]
    public void An_unnamed_organisation_gets_a_sentence_that_still_reads()
    {
        var message = Build(organizationName: null);

        // Not a placeholder standing in for a name. A tenant that has not filled
        // this in should send a plain invitation, not one signed "null" or
        // "Your Organisation Here".
        message.Subject.ShouldStartWith("اختبار تحديد المستوى — ");
        message.Body.ShouldContain("لقد تم إسناد اختبار");
        message.Body.ShouldContain("You have been assigned");
        message.Body.ShouldNotContain("—  ");
    }

    [Fact]
    public void The_accent_colour_is_the_tenants_when_it_is_a_colour()
    {
        Build(brandColor: "#b34700").Body.ShouldContain("background:#b34700");
        Build(brandColor: "#fff").Body.ShouldContain("background:#fff");
    }

    [Theory]
    [InlineData("red; background-image:url(https://tracker.example/pixel.png)")]
    [InlineData("\" onmouseover=\"alert(1)")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not-a-colour")]
    [InlineData("")]
    public void Anything_that_is_not_a_colour_falls_back(string supplied)
    {
        var body = Build(brandColor: supplied).Body;

        // The value goes straight into a style attribute in mail sent to
        // somebody else. A tenant administrator is trusted with their own
        // organisation, not with the inbox of a candidate they have never met —
        // and a tracking pixel smuggled in through a colour field would be sent
        // in our name.
        body.ShouldContain($"background:{InvitationEmail.DefaultBrandColor}");
        body.ShouldNotContain("tracker.example");
        body.ShouldNotContain("onmouseover");
    }

    [Fact]
    public void A_name_with_markup_in_it_cannot_rewrite_the_message()
    {
        var message = Build(
            candidateName: "<script>alert(1)</script>",
            examTitle: "Level <b>2</b>",
            organizationName: "<img src=x onerror=alert(1)>");

        // Names arrive from a spreadsheet somebody typed, and one stray angle
        // bracket must not restructure a message going out over the
        // organisation's name. The property is that nothing can open a tag —
        // the words survive as inert text between escaped brackets, so asserting
        // on "onerror=" rather than on "<img" would be testing the wrong thing.
        message.Body.ShouldNotContain("<script>");
        message.Body.ShouldNotContain("<img");
        message.Body.ShouldContain("&lt;script&gt;");
        message.Body.ShouldContain("&lt;img src=x onerror=alert(1)&gt;");

        // Escaped, but still legible: the candidate should see the name they
        // were entered under, oddities and all.
        message.Body.ShouldContain("&lt;b&gt;2&lt;/b&gt;");
    }

    [Fact]
    public void It_says_what_the_candidate_needs_before_they_start()
    {
        var message = Build();

        message.Body.ShouldContain(Url);
        message.Body.ShouldContain("45");                    // the time limit
        message.Body.ShouldContain("2026-09-04 17:00");      // when the link dies

        // The single fact that stops somebody opening the link on a train to see
        // what it is, and losing the attempt.
        message.Body.ShouldContain("لا يبدأ العدّ التنازلي");
        message.Body.ShouldContain("The timer does not start until you press start.");
    }

    private static InvitationEmail.Message Build(
        string? organizationName = "أكاديمية التداول",
        string? brandColor = "#0f6c8c",
        string candidateName = "سارة",
        string examTitle = "اختبار تحديد المستوى") =>
        InvitationEmail.Build(
            organizationName, brandColor, candidateName, examTitle, 45, Expires, Url);
}
