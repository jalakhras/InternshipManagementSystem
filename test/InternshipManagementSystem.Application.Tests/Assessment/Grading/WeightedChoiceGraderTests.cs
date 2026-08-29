using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.Grading.Graders;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Scoring a question where several answers are defensible and one is best.
/// <para>
/// The case this exists for: a trading question where closing the position is
/// sound, moving the stop is better, and adding to a loser is the thing the
/// question was written to catch. Binary scoring calls the first two equally
/// wrong, which measures nothing anyone wanted to measure.
/// </para>
/// </summary>
public class WeightedChoiceGraderTests
{
    private const decimal Marks = 10m;

    private static string WeightedSingle() => PayloadJson.Write(new ChoicePayload
    {
        Weighted = true,
        Options =
        [
            new OptionPayload { Id = "a", Text = "Close the full position", IsCorrect = false, Weight = 0.6m },
            new OptionPayload { Id = "b", Text = "Move the stop to break-even", IsCorrect = true, Weight = 1.0m },
            new OptionPayload { Id = "c", Text = "Add to the losing position", IsCorrect = false, Weight = -0.5m },
            new OptionPayload { Id = "d", Text = "Do nothing", IsCorrect = false, Weight = 0m },
        ],
    });

    private static string WeightedMulti() => PayloadJson.Write(new ChoicePayload
    {
        Weighted = true,
        Options =
        [
            new OptionPayload { Id = "a", Text = "Check the airway", IsCorrect = true, Weight = 0.4m },
            new OptionPayload { Id = "b", Text = "Check breathing", IsCorrect = true, Weight = 0.4m },
            new OptionPayload { Id = "c", Text = "Check circulation", IsCorrect = true, Weight = 0.2m },
            new OptionPayload { Id = "d", Text = "Medicate before assessing", IsCorrect = false, Weight = -0.6m },
        ],
    });

    // ------------------------------------------------------------ single choice

    [Fact]
    public void The_best_answer_earns_full_marks_and_counts_as_correct()
    {
        var result = new SingleChoiceGrader().Grade(WeightedSingle(), "\"b\"", Marks);

        result.AwardedScore.ShouldBe(10m);

        // IsCorrect keeps meaning "reached full marks", which is what the item
        // statistics and the reviewer's key both already assume.
        result.IsCorrect.ShouldBe(true);
    }

    [Fact]
    public void An_acceptable_answer_earns_its_share_and_does_not_count_as_correct()
    {
        var result = new SingleChoiceGrader().Grade(WeightedSingle(), "\"a\"", Marks);

        result.AwardedScore.ShouldBe(6m);
        result.IsCorrect.ShouldBe(false);
    }

    [Fact]
    public void A_harmful_answer_scores_zero_rather_than_less_than_nothing()
    {
        var result = new SingleChoiceGrader().Grade(WeightedSingle(), "\"c\"", Marks);

        // Priced at -0.5, floored at zero. A scoring system that leaves a taker
        // worse off than not answering measures their nerve, not their knowledge.
        result.AwardedScore.ShouldBe(0m);
        result.IsCorrect.ShouldBe(false);
    }

    [Fact]
    public void A_neutral_answer_scores_nothing()
    {
        new SingleChoiceGrader().Grade(WeightedSingle(), "\"d\"", Marks).AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void Selecting_two_options_still_fails_a_single_choice_question()
    {
        // Weighting changes how an answer is priced, not how many answers there are.
        new SingleChoiceGrader().Grade(WeightedSingle(), """["a","b"]""", Marks)
            .AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void An_option_the_payload_no_longer_knows_scores_nothing_rather_than_failing()
    {
        // A stored answer can outlive an edit to its question. That must produce a
        // zero, not a question nobody can score.
        var result = new SingleChoiceGrader().Grade(WeightedSingle(), "\"deleted\"", Marks);

        result.AwardedScore.ShouldBe(0m);
        result.NeedsManualReview.ShouldBeFalse();
    }

    [Fact]
    public void True_false_inherits_the_same_weighting()
    {
        new TrueFalseGrader().Grade(WeightedSingle(), "\"a\"", Marks).AwardedScore.ShouldBe(6m);
    }

    // ------------------------------------------------------------- multi select

    [Fact]
    public void The_best_set_earns_full_marks()
    {
        var result = new MultiSelectGrader().Grade(WeightedMulti(), """["a","b","c"]""", Marks);

        result.AwardedScore.ShouldBe(10m);
        result.IsCorrect.ShouldBe(true);
    }

    [Fact]
    public void An_incomplete_set_earns_what_it_picked()
    {
        var result = new MultiSelectGrader().Grade(WeightedMulti(), """["a","b"]""", Marks);

        // 0.4 + 0.4. Weighted mode is partial by construction, so AllowPartialCredit
        // has nothing to say here.
        result.AwardedScore.ShouldBe(8m);
        result.IsCorrect.ShouldBe(false);
    }

    [Fact]
    public void Selecting_everything_is_worse_than_choosing_carefully()
    {
        var everything = new MultiSelectGrader().Grade(WeightedMulti(), """["a","b","c","d"]""", Marks);
        var careful = new MultiSelectGrader().Grade(WeightedMulti(), """["a","b","c"]""", Marks);

        // The whole point of allowing a negative weight. Weighted mode switches off
        // the all-or-nothing rule, so this is the only thing standing between the
        // bank and the oldest exploit in multiple choice.
        everything.AwardedScore.ShouldBe(4m);
        everything.AwardedScore.ShouldBeLessThan(careful.AwardedScore);
    }

    [Fact]
    public void A_selection_of_only_harm_scores_zero()
    {
        new MultiSelectGrader().Grade(WeightedMulti(), """["d"]""", Marks).AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void Selecting_nothing_scores_zero()
    {
        new MultiSelectGrader().Grade(WeightedMulti(), "[]", Marks).AwardedScore.ShouldBe(0m);
    }

    // ------------------------------------------------- questions written before

    [Fact]
    public void A_question_without_weighting_grades_exactly_as_it_did()
    {
        // The compatibility guarantee, asserted rather than assumed: no weighted
        // flag, no weights, and a wrong pick still voids the question.
        var plain = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
            ],
        });

        new SingleChoiceGrader().Grade(plain, "\"a\"", Marks).AwardedScore.ShouldBe(10m);
        new SingleChoiceGrader().Grade(plain, "\"b\"", Marks).AwardedScore.ShouldBe(0m);
        new MultiSelectGrader().Grade(plain, """["a","b"]""", Marks).AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void An_unweighted_payload_carries_neither_new_field()
    {
        // Nullable on purpose: a question that does not use weighting must
        // serialise byte-for-byte as it does today.
        var json = PayloadJson.Write(new ChoicePayload
        {
            Options = [new OptionPayload { Id = "a", Text = "Only", IsCorrect = true }],
        });

        json.ShouldNotContain("weighted");
        json.ShouldNotContain("weight");
    }
}
