using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Issues and reads the short-lived credential a taker holds during one attempt.
/// <para>
/// The old flow marked the start endpoint <c>[AllowAnonymous]</c> and had it call a
/// service guarded by an administrative permission, so it returned 403 for every
/// candidate — the product's central feature could not run at all.
/// </para>
/// <para>
/// The fix is not to loosen those permissions. A link token is exchanged once for a
/// token scoped to a single attempt, and the exam endpoints authorise against that.
/// A taker therefore never touches the staff permission system, and the credential
/// grants nothing beyond the one attempt it names.
/// </para>
/// </summary>
public class ExamSessionTokenService : ISingletonDependency
{
    public const string SchemeName = "ExamSession";

    public const string ClaimAttemptId = "ims:attempt";
    public const string ClaimCandidateId = "ims:candidate";
    public const string ClaimExamId = "ims:exam";
    public const string ClaimTenantId = "ims:tenant";

    /// <summary>
    /// Which link this session came from.
    /// <para>
    /// Carried because a candidate can hold more than one link to the same exam —
    /// a resit is exactly that — and without it the start had to guess, resolving
    /// by candidate and exam and taking whichever row the database returned
    /// first. A student opening their second link could burn an attempt on the
    /// first one.
    /// </para>
    /// </summary>
    public const string ClaimLinkId = "ims:link";

    /// <summary>
    /// Shorter than this and a key is guessable by someone who knows it is a
    /// pass phrase rather than random bytes.
    /// </summary>
    private const int MinimumKeyLength = 32;

    private readonly SymmetricSecurityKey _key;

    public ExamSessionTokenService(IConfiguration configuration)
    {
        // No fallback, and no null check.
        //
        // This used to fall back to the app's encryption pass phrase "so a dev
        // machine works out of the box", guarded by `?? throw`. That guard tests
        // for null. The pass phrase is committed as an empty string, which is not
        // null — so every exam session in every environment was signed with
        // SHA-256 of the empty string, the most published hash constant there is.
        // Anyone could mint a token for any attempt in any tenant.
        //
        // A signing key is not a convenience, and sharing one with the
        // string-encryption secret is its own mistake: two different purposes,
        // two different rotation schedules, one compromise.
        var secret = configuration["ExamSession:SigningKey"];

        if (string.IsNullOrWhiteSpace(secret) || secret.Trim().Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"ExamSession:SigningKey must be set to at least {MinimumKeyLength} characters before " +
                "exam sessions can be issued. It signs the credential that lets a candidate reach " +
                "their attempt, so a weak or shared value is a way into every attempt in every tenant.");
        }

        _key = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    /// <summary>
    /// Mints a credential for one attempt. It expires with the attempt's own
    /// deadline plus a short grace period, so it cannot outlive the exam it was
    /// issued for.
    /// </summary>
    public string Issue(
        Guid attemptId,
        Guid candidateId,
        Guid examId,
        Guid? tenantId,
        DateTime deadlineUtc,
        Guid linkId = default)
    {
        var claims = new List<Claim>
        {
            new(ClaimAttemptId, attemptId.ToString()),
            new(ClaimCandidateId, candidateId.ToString()),
            new(ClaimExamId, examId.ToString()),
            new(ClaimLinkId, linkId.ToString())
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(ClaimTenantId, tenantId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: "ims-exam-session",
            audience: "ims-exam-session",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            // Grace so the final submit still authenticates as the clock hits zero.
            expires: deadlineUtc.AddMinutes(5),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Validates a credential and returns what it asserts, or null when it does not hold.</summary>
    public ExamSessionClaims? Read(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "ims-exam-session",
                ValidateAudience = true,
                ValidAudience = "ims-exam-session",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var attemptId = principal.FindFirst(ClaimAttemptId)?.Value;
            var candidateId = principal.FindFirst(ClaimCandidateId)?.Value;
            var examId = principal.FindFirst(ClaimExamId)?.Value;
            var tenantId = principal.FindFirst(ClaimTenantId)?.Value;
            var linkId = principal.FindFirst(ClaimLinkId)?.Value;

            if (attemptId is null || candidateId is null || examId is null)
            {
                return null;
            }

            return new ExamSessionClaims(
                Guid.Parse(attemptId),
                Guid.Parse(candidateId),
                Guid.Parse(examId),
                tenantId is null ? null : Guid.Parse(tenantId),
                // Absent on a token minted before this claim existed. Those sessions
                // fall back to the old resolution rather than being rejected: an
                // exam already in progress must not end because we shipped.
                linkId is null ? null : Guid.Parse(linkId));
        }
        catch (Exception)
        {
            // Expired, tampered with, or malformed — all mean the same to the caller.
            return null;
        }
    }

    /// <summary>Hashes a link token for storage and lookup. The plain token exists only in the email.</summary>
    public static string HashLinkToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>Generates a link token with 256 bits of entropy, URL-safe.</summary>
    public static string NewLinkToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
               .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    /// <summary>Seed for an attempt's shuffling. Random per attempt, then persisted.</summary>
    public static int NewShuffleSeed() =>
        BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4)) & int.MaxValue;
}

/// <summary>What an exam-session credential asserts.</summary>
public sealed record ExamSessionClaims(
    Guid AttemptId,
    Guid CandidateId,
    Guid ExamId,
    Guid? TenantId,
    Guid? LinkId = null);
