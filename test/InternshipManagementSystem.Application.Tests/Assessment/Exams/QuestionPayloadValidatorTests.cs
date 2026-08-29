using System.Linq;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// Every case here is a question that would otherwise be discovered mid-exam.
/// <para>
/// The payload is free-form JSON, which is what makes a new question type cost one
/// class instead of a migration. The price is that nothing structural stops an
/// author saving something no grader can read — and the moment that surfaces is
/// while a candidate is sitting the paper, which is the worst possible moment.
/// </para>
/// </summary>
public class QuestionPayloadValidatorTests
{
    private readonly QuestionPayloadValidator _validator = new();

    [Fact]
    public void Single_choice_with_no_correct_option_is_refused()
    {
        // Would score every candidate zero, and look like a hard question rather
        // than a broken one.
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "A", IsCorrect = false },
                new OptionPayload { Id = "b", Text = "B", IsCorrect = false },
            ],
        });

        _validator.Blocking(QuestionTypes.SingleChoice, payload)
            .ShouldContain("IMS:Question:NoCorrectOption");
    }

    [Fact]
    public void Single_choice_with_two_correct_options_is_refused()
    {
        // The grader requires exactly one selection, so whichever the candidate
        // picks is marked wrong. Nobody can pass this question.
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "A", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "B", IsCorrect = true },
            ],
        });

        _validator.Blocking(QuestionTypes.SingleChoice, payload)
            .ShouldContain("IMS:Question:SingleChoiceHasManyCorrect");
    }

    [Fact]
    public void Duplicate_option_ids_are_refused()
    {
        // Options are shuffled by id and answers are stored by id. Duplicates make
        // a saved answer ambiguous, and the ambiguity only shows up at grading.
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "First", IsCorrect = true },
                new OptionPayload { Id = "a", Text = "Second", IsCorrect = false },
            ],
        });

        _validator.Blocking(QuestionTypes.SingleChoice, payload)
            .ShouldContain("IMS:Question:DuplicateOptionId");
    }

    [Fact]
    public void Multi_select_with_every_option_correct_is_refused()
    {
        // Selecting everything would be right, so the question measures nothing —
        // and it is the exact shape the old scoring bug rewarded.
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "A", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "B", IsCorrect = true },
            ],
        });

        _validator.Blocking(QuestionTypes.MultiSelect, payload)
            .ShouldContain("IMS:Question:AllOptionsCorrect");
    }

    [Fact]
    public void Ordering_positions_must_be_a_complete_sequence()
    {
        // Partial credit divides by the item count and compares against position.
        // A gap means the arithmetic is against a sequence that does not exist.
        var payload = PayloadJson.Write(new OrderingPayload
        {
            Items =
            [
                new OrderingItem { Id = "a", Text = "First", CorrectPosition = 0 },
                new OrderingItem { Id = "b", Text = "Third", CorrectPosition = 2 },
            ],
        });

        _validator.Blocking(QuestionTypes.Ordering, payload)
            .ShouldContain("IMS:Question:OrderingPositionsNotSequential");
    }

    [Fact]
    public void Hotspot_without_a_correct_region_is_refused()
    {
        var payload = PayloadJson.Write(new HotspotPayload
        {
            ImageBlobName = "chart.png",
            Regions = [new HotspotRegion { Id = "r", X = 1, Y = 1, Width = 5, Height = 5, IsCorrect = false }],
        });

        _validator.Blocking(QuestionTypes.Hotspot, payload)
            .ShouldContain("IMS:Question:NoCorrectRegion");
    }

    [Fact]
    public void Blank_with_no_accepted_answer_is_refused()
    {
        var payload = PayloadJson.Write(new FillInTheBlankPayload
        {
            Blanks = [new BlankSpec { Id = "b1", AcceptedAnswers = [] }],
        });

        _validator.Blocking(QuestionTypes.FillInTheBlank, payload)
            .ShouldContain("IMS:Question:BlankHasNoAnswer");
    }

    [Fact]
    public void Negative_numeric_tolerance_is_refused()
    {
        // A negative tolerance makes the accepted range empty, so the exact right
        // answer is marked wrong.
        var payload = PayloadJson.Write(new NumericPayload { CorrectValue = 10m, Tolerance = -1m });

        _validator.Blocking(QuestionTypes.Numeric, payload)
            .ShouldContain("IMS:Question:NegativeTolerance");
    }

    [Fact]
    public void Code_without_expected_output_is_allowed_but_reported()
    {
        // Legitimate — it just means a human marks every submission. Worth saying
        // at authoring time rather than discovering as a full review queue.
        var payload = PayloadJson.Write(new CodePayload { Language = "csharp" });

        _validator.Blocking(QuestionTypes.Code, payload).ShouldBeEmpty();

        _validator.Validate(QuestionTypes.Code, payload)
            .ShouldContain("IMS:Question:CodeWithoutExpectedOutputIsManual");
    }

    [Fact]
    public void An_unknown_type_is_allowed_and_reported_as_manual()
    {
        // Extensibility is the point of the payload, so a type this build does not
        // know is not rejected — but the author is told it will be marked by hand,
        // which is what the grader resolver will do with it.
        _validator.Blocking("some-future-type", "{}").ShouldBeEmpty();

        _validator.Validate("some-future-type", "{}")
            .ShouldContain("IMS:Question:UnknownTypeWillBeManual");
    }

    [Fact]
    public void A_well_formed_question_passes()
    {
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "Support", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "Resistance", IsCorrect = false },
            ],
        });

        _validator.Validate(QuestionTypes.SingleChoice, payload).ShouldBeEmpty();
    }

    // ---------------------------------------------------- weighted scoring

    private static string Weighted(params (string Id, bool Correct, decimal? Weight)[] options) =>
        PayloadJson.Write(new ChoicePayload
        {
            Weighted = true,
            Options = options
                .Select(o => new OptionPayload { Id = o.Id, Text = o.Id, IsCorrect = o.Correct, Weight = o.Weight })
                .ToList(),
        });

    [Fact]
    public void A_weighted_option_without_a_weight_is_refused()
    {
        // Treating the gap as zero would turn an unfinished edit into a quietly
        // harsher question, with nothing on the payload to show it happened.
        _validator.Blocking(QuestionTypes.SingleChoice, Weighted(("a", true, 1m), ("b", false, null)))
            .ShouldContain("IMS:Question:WeightMissing");
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-2)]
    public void A_weight_outside_minus_one_to_one_is_refused(double weight)
    {
        _validator.Blocking(QuestionTypes.SingleChoice, Weighted(("a", true, 1m), ("b", false, (decimal)weight)))
            .ShouldContain("IMS:Question:WeightOutOfRange");
    }

    [Fact]
    public void The_correct_flag_and_a_weight_of_one_must_agree()
    {
        // One canonical best answer. Two sources of truth drift, and then the
        // reviewer's key and the grader disagree about the same question.
        _validator.Blocking(QuestionTypes.SingleChoice, Weighted(("a", true, 0.5m), ("b", false, 0m)))
            .ShouldContain("IMS:Question:WeightConflictsWithCorrectFlag");

        _validator.Blocking(QuestionTypes.SingleChoice, Weighted(("a", false, 1m), ("b", false, 0m)))
            .ShouldContain("IMS:Question:WeightConflictsWithCorrectFlag");
    }

    [Fact]
    public void A_weighted_multi_select_needs_something_priced_below_zero()
    {
        // Otherwise selecting every option scores at least as well as choosing
        // carefully — the oldest exploit in multiple choice, arriving through the
        // door that weighted mode opens by switching off the all-or-nothing rule.
        _validator.Blocking(QuestionTypes.MultiSelect, Weighted(("a", true, 1m), ("b", false, 0.5m)))
            .ShouldContain("IMS:Question:AllWeightsPositive");

        _validator.Blocking(QuestionTypes.MultiSelect, Weighted(("a", true, 1m), ("b", false, -0.5m)))
            .ShouldNotContain("IMS:Question:AllWeightsPositive");
    }

    [Fact]
    public void A_single_choice_question_may_have_no_penalty_at_all()
    {
        // One pick means selecting everything is not available, so nothing needs
        // pricing below zero to close it.
        _validator.Blocking(QuestionTypes.SingleChoice, Weighted(("a", true, 1m), ("b", false, 0.4m)))
            .ShouldBeEmpty();
    }

    [Fact]
    public void None_of_the_weight_rules_touch_a_question_that_does_not_use_them()
    {
        var plain = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false },
            ],
        });

        _validator.Blocking(QuestionTypes.SingleChoice, plain).ShouldBeEmpty();
        _validator.Blocking(QuestionTypes.MultiSelect, plain).ShouldBeEmpty();
    }
}
