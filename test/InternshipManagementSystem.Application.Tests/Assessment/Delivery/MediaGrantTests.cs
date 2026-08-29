using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// The grant that lets a candidate's browser fetch one stored file.
/// <para>
/// It travels in the URL, which is the whole difficulty: a candidate has no
/// account, and nothing in the page gets to add a header to the request an
/// <c>&lt;img&gt;</c> or an <c>&lt;audio&gt;</c> element makes. So the address is
/// the credential, and an address is the thing people copy, paste and forward.
/// </para>
/// <para>
/// Which makes the negatives the point of this file. A grant has to be worth
/// exactly one blob for exactly as long as the sitting.
/// </para>
/// </summary>
public class MediaGrantTests
{
    private readonly ExamSessionTokenService _tokens = Service();

    private const string Clip = "tenant/8a1f0b3c4d5e6f708192a3b4c5d6e7f8.mp3";

    [Fact]
    public void A_grant_opens_the_blob_it_names()
    {
        var grant = _tokens.IssueMediaGrant(Clip, DateTime.UtcNow.AddMinutes(30));

        _tokens.GrantsMedia(grant, Clip).ShouldBeTrue();
    }

    [Fact]
    public void A_grant_does_not_open_a_different_blob()
    {
        var grant = _tokens.IssueMediaGrant(Clip, DateTime.UtcNow.AddMinutes(30));

        // The listening clip and somebody's uploaded answer sit in one container,
        // and both are reached by name. A grant that opened the container rather
        // than the file would hand every candidate everyone else's work.
        _tokens.GrantsMedia(grant, "tenant/ffffffffffffffffffffffffffffffff.pdf").ShouldBeFalse();
    }

    [Fact]
    public void An_expired_grant_opens_nothing()
    {
        var grant = _tokens.IssueMediaGrant(Clip, DateTime.UtcNow.AddHours(-2));

        // A URL copied out of the page during the exam has to stop working once the
        // exam is over, or the paper leaks by the ordinary act of sharing a link.
        _tokens.GrantsMedia(grant, Clip).ShouldBeFalse();
    }

    [Fact]
    public void A_grant_signed_with_another_key_opens_nothing()
    {
        var elsewhere = Service("a-completely-different-signing-key-value");

        _tokens.GrantsMedia(elsewhere.IssueMediaGrant(Clip, DateTime.UtcNow.AddMinutes(30)), Clip)
            .ShouldBeFalse();
    }

    [Fact]
    public void An_exam_session_token_is_not_a_media_grant()
    {
        // Same key, different audience. Without that separation the credential that
        // runs the sitting would also be a bearer token for the blob container, and
        // the one place it is exposed is the address bar.
        var session = _tokens.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow.AddHours(1));

        _tokens.GrantsMedia(session, Clip).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-token")]
    public void Nonsense_opens_nothing(string? grant)
    {
        _tokens.GrantsMedia(grant, Clip).ShouldBeFalse();
    }

    private static ExamSessionTokenService Service(
        string key = "test-only-signing-key-not-for-any-real-deployment") =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExamSession:SigningKey"] = key })
            .Build());
}
