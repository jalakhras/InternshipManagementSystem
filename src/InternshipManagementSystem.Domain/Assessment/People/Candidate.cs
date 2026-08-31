using System;
using System.Collections.Generic;
using InternshipManagementSystem.Assessment;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// A person being assessed. Deliberately not an <c>IdentityUser</c>: they reach an
/// exam through a signed link, not a login, which removes the sign-up step and stops
/// the tenant accumulating dormant accounts.
/// <para>
/// The tenant decides what this person is called in its own UI — candidate, student,
/// trainee. See <c>CategorySet</c>.
/// </para>
/// <para>
/// This replaces the old parallel <c>Candidate</c> / <c>Trainee</c> pair, which
/// carried two entity chains and two grading services for the same concept.
/// </para>
/// </summary>
public class Candidate : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    private string _fullName = default!;

    /// <summary>The name as the organisation wrote it, and what every screen shows.</summary>
    public string FullName
    {
        get => _fullName;
        set
        {
            _fullName = value;
            NormalisedName = ArabicText.Normalise(value).ToLowerInvariant();
        }
    }

    /// <summary>
    /// The same name with the spellings that do not change it folded away, so it
    /// can be searched for.
    /// <para>
    /// Arabic writes one name several ways: four spellings of alef, a final ha
    /// typed either <c>ة</c> or <c>ه</c>, and optional vowel marks nobody types
    /// consistently. Those marks are the hard case, and they are not a collation
    /// problem: a database matches a substring positionally, and a fatha sits
    /// <em>between</em> two letters — so «مُحَمَّد» cannot be found by «محمد»
    /// under any collation at all. It was tried, on the real server, against
    /// <c>Arabic_CI_AI</c> and <c>Latin1_General_CI_AI</c>. Both say no.
    /// </para>
    /// <para>
    /// Written from the setter above rather than by whoever remembers, because a
    /// search column that can drift from the thing it indexes is worse than none:
    /// it finds people who have been renamed and misses people who have not.
    /// Lower-cased for the same reason — the test provider is case-sensitive and
    /// the production one is not, so a search that worked in one would not in the
    /// other.
    /// </para>
    /// </summary>
    public string NormalisedName { get; private set; } = string.Empty;
    public string Email { get; set; } = default!;
    public string? PhoneNumber { get; set; }

    /// <summary>What they applied for, are enrolled in, or are being assessed against.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Free-text detail the tenant wants to keep: a job title, a course name.</summary>
    public string? Reference { get; set; }


    /// <summary>Set when the tenant also gave this person a login. Usually null.</summary>
    public Guid? UserId { get; set; }

    public ICollection<CandidateGroupMember> GroupMemberships { get; set; } = new List<CandidateGroupMember>();

    protected Candidate() { }

    public Candidate(Guid id, Guid? tenantId, string fullName, string email) : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
    }
}
