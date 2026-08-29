using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.Grading.Graders;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Guards the scoring defect that let anyone score full marks.
/// <para>
/// The previous implementation computed <c>|correct ∩ selected| / |correct|</c> and
/// never looked at whether a selection was wrong, so ticking every box on a
/// multi-select question always produced a perfect score. These tests exist so that
/// cannot come back.
/// </para>
/// </summary>
public class MultiSelectGraderTests
{
    private readonly MultiSelectGrader _grader = new();

    /// <summary>Three options, A and B correct, C wrong.</summary>
    private static string Payload(bool allowPartialCredit = true) => PayloadJson.Write(new ChoicePayload
    {
        AllowPartialCredit = allowPartialCredit,
        Options =
        [
            new OptionPayload { Id = "a", Text = "A", IsCorrect = true },
            new OptionPayload { Id = "b", Text = "B", IsCorrect = true },
            new OptionPayload { Id = "c", Text = "C", IsCorrect = false }
        ]
    });

    private static string Selection(params string[] ids) => PayloadJson.Write(ids);

    [Fact]
    public void Selecting_every_option_scores_zero()
    {
        // The exact exploit: tick everything, and the old code awarded full marks.
        var result = _grader.Grade(Payload(), Selection("a", "b", "c"), 10m);

        result.AwardedScore.ShouldBe(0m);
        result.IsCorrect.ShouldBe(false);
    }

    [Fact]
    public void Exactly_the_correct_options_scores_full_marks()
    {
        var result = _grader.Grade(Payload(), Selection("a", "b"), 10m);

        result.AwardedScore.ShouldBe(10m);
        result.IsCorrect.ShouldBe(true);
    }

    [Fact]
    public void Half_the_correct_options_and_nothing_wrong_scores_half()
    {
        var result = _grader.Grade(Payload(), Selection("a"), 10m);

        result.AwardedScore.ShouldBe(5m);
        result.IsCorrect.ShouldBe(false);
    }

    [Fact]
    public void One_wrong_option_voids_an_otherwise_complete_answer()
    {
        // Both correct options are present, but so is a wrong one. Partial credit
        // must not rescue this, or "select everything" becomes optimal again.
        var result = _grader.Grade(Payload(), Selection("a", "b", "c"), 10m);

        result.AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void Partial_credit_off_means_all_or_nothing()
    {
        var result = _grader.Grade(Payload(allowPartialCredit: false), Selection("a"), 10m);

        result.AwardedScore.ShouldBe(0m);
        result.IsCorrect.ShouldBe(false);
    }

    [Fact]
    public void No_selection_scores_zero()
    {
        _grader.Grade(Payload(), Selection(), 10m).AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void Unreadable_payload_goes_to_a_human_rather_than_scoring_zero()
    {
        // An authoring mistake must not silently cost the taker marks.
        var result = _grader.Grade("{ not json", Selection("a"), 10m);

        result.NeedsManualReview.ShouldBeTrue();
    }
}
