using System;
using System.Linq.Expressions;

namespace InternshipManagementSystem.Assessment.People;

/// <summary>
/// One definition of what it means to search for a person.
/// <para>
/// Four screens let somebody type a name into a box — the roll, the results
/// roster, the running-sittings monitor, and the send panel's person picker —
/// and each had written the comparison out for itself. Three of them searched
/// the raw name only, so a coordinator who found «مُحَمَّد» on one screen could
/// not find them on the next.
/// </para>
/// <para>
/// A search that answers differently depending on which box you typed into is
/// not a search anybody learns to trust, so the rule lives in one place and the
/// screens ask for it.
/// </para>
/// </summary>
public static class CandidateSearch
{
    /// <summary>
    /// Everything a person might have been given: their name, their address, and
    /// whatever reference the organisation keeps for them.
    /// <para>
    /// Always <c>Contains</c>, never a prefix match. Somebody looking a person up
    /// has a fragment — a family name, the middle of an address, the tail of a
    /// student number — and a search that only matches from the first character
    /// refuses all three.
    /// </para>
    /// <para>
    /// The folded form of the name is compared alongside the raw one. Arabic
    /// writes a name several ways without changing it, and the vowel marks in
    /// particular are unreachable by any collation: they are characters sitting
    /// between the letters, and a database matches a substring positionally.
    /// </para>
    /// </summary>
    public static Expression<Func<Candidate, bool>> Matching(string term)
    {
        var trimmed = term.Trim();
        var folded = ArabicText.Normalise(trimmed).ToLowerInvariant();

        return candidate =>
            candidate.FullName.Contains(trimmed)
            || (folded != "" && candidate.NormalisedName.Contains(folded))
            || candidate.Email.Contains(trimmed)
            || (candidate.Reference != null && candidate.Reference.Contains(trimmed));
    }
}
