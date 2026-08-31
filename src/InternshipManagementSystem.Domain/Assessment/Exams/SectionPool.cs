using System;
using System.Collections.Generic;
using System.Linq;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// What one part of a paper is allowed to draw from.
/// <para>
/// Three places need this answer and must never disagree: the builder that
/// assembles a candidate's paper, the publish check that refuses an exam whose
/// parts cannot be filled, and the count shown next to a rule while the author
/// is still writing it. Written out three times, they drift — and the shape the
/// drift takes is the worst one available: a publish check that says the paper
/// can be built, and a paper that then comes up short in front of a candidate.
/// </para>
/// <para>
/// Precedence is deliberate. A section with questions filed under it draws from
/// those and nothing else — somebody put them there on purpose. A section with
/// nothing filed draws from the shared bank, on whatever it says about itself:
/// its rules if it owns any, otherwise its topic.
/// </para>
/// </summary>
public static class SectionPool
{
    /// <param name="section">The part of the paper being filled.</param>
    /// <param name="sectionRules">The blueprint rules aimed at this section.</param>
    /// <param name="bank">Every question the exam can draw, filed or shared.</param>
    /// <param name="taken">
    /// Questions an earlier section already drew, so two parts of one paper never
    /// ask the same question twice.
    /// </param>
    public static List<Question> For(
        ExamSection section,
        IReadOnlyList<ExamBlueprintRule> sectionRules,
        IReadOnlyList<Question> bank,
        ISet<Guid>? taken = null)
    {
        var filed = bank.Where(q => q.IsActive && q.ExamSectionId == section.Id).ToList();

        if (filed.Count > 0)
        {
            return filed;
        }

        // A question in the shared bank belongs to every exam at its level, so it
        // cannot be filed into one exam's part: filing it there would be a claim
        // about a paper it has never seen. What it says about itself — its topic,
        // its difficulty, its type — is true everywhere, and that is what a part
        // of the paper selects on.
        var drawable = bank.Where(q => q.IsActive
                                       && q.ExamSectionId is null
                                       && (taken is null || !taken.Contains(q.Id)));

        if (sectionRules.Count > 0)
        {
            return drawable.Where(q => sectionRules.Any(rule => rule.Matches(q))).ToList();
        }

        return section.TopicId is { } topicId
            ? drawable.Where(q => q.TopicId == topicId).ToList()
            : [];
    }
}
