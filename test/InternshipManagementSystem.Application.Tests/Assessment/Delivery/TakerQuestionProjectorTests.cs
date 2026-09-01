using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InternshipManagementSystem.Assessment.Delivery;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Grading;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Guards the leak that made every other anti-cheating measure pointless.
/// <para>
/// The old <c>QuestionDto</c> was the only question DTO in the system and carried
/// <c>CorrectAnswer</c> and <c>CodeExpectedOutput</c>, so the answer key was sent to
/// the browser with the question. Blocking developer tools would have changed
/// nothing: the keys were in the payload.
/// </para>
/// <para>
/// Each test serialises the projection and asserts on the wire format, because what
/// matters is what actually crosses — not what the object model intends.
/// </para>
/// </summary>
public class TakerQuestionProjectorTests
{
    private readonly TakerQuestionProjector _projector = new();

    private static string Wire(object dto) => JsonSerializer.Serialize(dto, PayloadJson.Options);

    private static AttemptQuestion Slot(Guid questionId, string? optionOrder = null) =>
        new(Guid.NewGuid(), null, Guid.NewGuid(), questionId, 0, 5m) { OptionOrder = optionOrder };

    private static Question Question(string type, string payload) =>
        new(Guid.NewGuid(), null, Guid.NewGuid(), type, "Prompt") { Score = 5m, Explanation = "Because." };

    [Fact]
    public void Choice_options_cross_the_wire_without_their_correctness()
    {
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false }
            ]
        });

        var question = Question(QuestionTypes.SingleChoice, payload);
        question.Payload = payload;

        var dto = _projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b);

        dto.Options.Count.ShouldBe(2);
        Wire(dto).ShouldNotContain("isCorrect");
        Wire(dto).ShouldNotContain("IsCorrect");
    }

    [Fact]
    public void Code_questions_send_the_starter_but_never_the_expected_output()
    {
        var payload = PayloadJson.Write(new CodePayload
        {
            Language = "csharp",
            StarterTemplate = "// write here",

            // Long and distinctive on purpose. This was "42", and the assertion
            // below searches the whole serialised payload — so the test failed
            // roughly one run in three, when the question's random id happened to
            // contain those two digits.
            ExpectedOutput = "forty-two-and-nothing-else"
        });

        var question = Question(QuestionTypes.Code, payload);
        question.Payload = payload;

        var dto = _projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b);
        var wire = Wire(dto);

        wire.ShouldContain("write here");
        wire.ShouldNotContain("forty-two-and-nothing-else");
        wire.ShouldNotContain("expectedOutput");

        // And that this question is marked by comparing the program's output —
        // said as a flag, never as the output. Note how close the two names
        // read: `expectsOutput` is the flag and goes; `expectedOutput` is the
        // answer and stays. Both are asserted here so neither can be quietly
        // turned into the other.
        dto.Display["expectsOutput"].ShouldBe(true);
    }

    [Fact]
    public void A_code_question_with_no_expected_output_says_so()
    {
        var payload = PayloadJson.Write(new CodePayload
        {
            Language = "python",
            StarterTemplate = "# describe your approach",
        });

        var question = Question(QuestionTypes.Code, payload);
        question.Payload = payload;

        var dto = _projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b);

        // A question asking for an approach rather than a program has no single
        // output and goes to a person. The candidate is asked for code, and must
        // not be told to write what it prints — the whole point of sending this
        // is that "write the program" and "write what it prints" are different
        // instructions, and answering the wrong one scores nothing.
        dto.Display["expectsOutput"].ShouldBe(false);
    }

    [Fact]
    public void Numeric_questions_send_the_unit_but_never_the_value_or_tolerance()
    {
        var payload = PayloadJson.Write(new NumericPayload
        {
            CorrectValue = 1234.5m,
            Tolerance = 0.5m,
            Unit = "USD"
        });

        var question = Question(QuestionTypes.Numeric, payload);
        question.Payload = payload;

        var wire = Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b));

        wire.ShouldContain("USD");
        wire.ShouldNotContain("1234.5");
        wire.ShouldNotContain("correctValue");
    }

    [Fact]
    public void Hotspot_questions_send_the_image_but_never_the_target_regions()
    {
        // Sending regions would literally draw the answer on the taker's screen.
        var payload = PayloadJson.Write(new HotspotPayload
        {
            ImageBlobName = "chart.png",
            Regions =
            [
                new HotspotRegion { Id = "r1", X = 10, Y = 20, Width = 5, Height = 5, IsCorrect = true, Label = "Support" }
            ]
        });

        var question = Question(QuestionTypes.Hotspot, payload);
        question.Payload = payload;

        var wire = Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b));

        wire.ShouldContain("chart.png");
        wire.ShouldNotContain("Support");
        wire.ShouldNotContain("isCorrect");
    }

    [Fact]
    public void Fill_in_the_blank_sends_blank_ids_but_never_accepted_answers()
    {
        var payload = PayloadJson.Write(new FillInTheBlankPayload
        {
            Blanks = [new BlankSpec { Id = "b1", AcceptedAnswers = ["photosynthesis"] }]
        });

        var question = Question(QuestionTypes.FillInTheBlank, payload);
        question.Payload = payload;

        var wire = Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b));

        wire.ShouldContain("b1");
        wire.ShouldNotContain("photosynthesis");
    }

    [Fact]
    public void Ordering_sends_the_items_but_never_their_correct_positions()
    {
        var payload = PayloadJson.Write(new OrderingPayload
        {
            Items =
            [
                new OrderingItem { Id = "s1", Text = "Second step", CorrectPosition = 1 },
                new OrderingItem { Id = "s2", Text = "First step", CorrectPosition = 0 }
            ]
        });

        var question = Question(QuestionTypes.Ordering, payload);
        question.Payload = payload;

        var wire = Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b));

        wire.ShouldContain("First step");
        wire.ShouldNotContain("correctPosition");
    }

    [Fact]
    public void Rubric_criteria_are_shown_but_reviewer_guidance_stays_behind()
    {
        var payload = PayloadJson.Write(new RubricPayload
        {
            ReviewerGuidance = "Award full marks only if they mention risk.",
            Criteria = [new RubricCriterion { Id = "c1", Name = "Clarity", MaxScore = 3m }]
        });

        var question = Question(QuestionTypes.Text, payload);
        question.Payload = payload;

        var wire = Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b));

        wire.ShouldContain("Clarity");
        wire.ShouldNotContain("mention risk");
    }

    [Fact]
    public void The_explanation_never_travels_during_an_attempt()
    {
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options = [new OptionPayload { Id = "a", Text = "A", IsCorrect = true }]
        });

        var question = Question(QuestionTypes.SingleChoice, payload);
        question.Payload = payload;

        // Practice mode reveals this, but only after submission and through a
        // different projection.
        Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b))
            .ShouldNotContain("Because.");
    }

    [Fact]
    public void An_unknown_type_exposes_nothing_but_the_prompt()
    {
        // Failing closed: a type added later cannot leak its key by default.
        var question = Question("some-future-type", "{\"secretKey\":\"leaked\"}");
        question.Payload = "{\"secretKey\":\"leaked\"}";

        var dto = _projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b);

        dto.Display.ShouldBeEmpty();
        Wire(dto).ShouldNotContain("leaked");
    }

    [Fact]
    public void Options_follow_this_takers_frozen_order()
    {
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Options =
            [
                new OptionPayload { Id = "a", Text = "A", IsCorrect = true },
                new OptionPayload { Id = "b", Text = "B", IsCorrect = false },
                new OptionPayload { Id = "c", Text = "C", IsCorrect = false }
            ]
        });

        var question = Question(QuestionTypes.SingleChoice, payload);
        question.Payload = payload;

        var order = PayloadJson.Write(new List<string> { "c", "a", "b" });
        var dto = _projector.Project(question, Slot(question.Id, order), null, 1, b => "/media/" + b);

        dto.Options.Select(o => o.Id).ShouldBe(new[] { "c", "a", "b" });
    }

    /// <summary>
    /// The grader marks a multi-select by one of three quite different rules, and
    /// which one it is decides how somebody should answer: under <c>exact</c>,
    /// ticking a fourth box you are unsure of when three are right costs the whole
    /// mark. None of that reached the person answering.
    /// </summary>
    [Theory]
    [InlineData(false, false, "exact")]
    [InlineData(true, false, "partial")]
    [InlineData(false, true, "weighted")]
    public void A_multi_select_says_how_it_will_be_marked(bool partial, bool weighted, string expected)
    {
        var payload = PayloadJson.Write(new ChoicePayload
        {
            AllowPartialCredit = partial,
            Weighted = weighted ? true : null,
            Options =
            [
                new OptionPayload { Id = "a", Text = "Right", IsCorrect = true, Weight = 1m },
                new OptionPayload { Id = "b", Text = "Also right", IsCorrect = true, Weight = 1m },
                new OptionPayload { Id = "c", Text = "Harmful", IsCorrect = false, Weight = -1m }
            ]
        });

        var question = Question(QuestionTypes.MultiSelect, payload);
        question.Payload = payload;

        var dto = _projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b);

        dto.Display["scoring"].ShouldBe(expected);
    }

    [Fact]
    public void Saying_how_a_multi_select_is_marked_does_not_say_what_the_answer_is()
    {
        var payload = PayloadJson.Write(new ChoicePayload
        {
            Weighted = true,
            Options =
            [
                new OptionPayload { Id = "a", Text = "Right", IsCorrect = true, Weight = 0.75m },
                new OptionPayload { Id = "b", Text = "Harmful", IsCorrect = false, Weight = -0.5m }
            ]
        });

        var question = Question(QuestionTypes.MultiSelect, payload);
        question.Payload = payload;

        var wire = Wire(_projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b));

        // The rule crosses.
        wire.ShouldContain("weighted");

        // Nothing that answers the question does. A weight is the answer written
        // as a number: whoever reads 0.75 beside one option and -0.5 beside
        // another has been handed the key.
        wire.ShouldNotContain("isCorrect");
        wire.ShouldNotContain("0.75");
        wire.ShouldNotContain("-0.5");
        wire.ShouldNotContain("weight\"");
    }

    [Fact]
    public void Only_a_multi_select_is_marked_by_a_rule_worth_stating()
    {
        // Single choice and true/false have one rule and it is the obvious one.
        // Printing a sentence about marking under every question would train
        // people to skip the sentence, which is how the one that matters gets
        // skipped too.
        foreach (var type in new[] { QuestionTypes.SingleChoice, QuestionTypes.TrueFalse })
        {
            var payload = PayloadJson.Write(new ChoicePayload
            {
                Options =
                [
                    new OptionPayload { Id = "a", Text = "Right", IsCorrect = true },
                    new OptionPayload { Id = "b", Text = "Wrong", IsCorrect = false }
                ]
            });

            var question = Question(type, payload);
            question.Payload = payload;

            var dto = _projector.Project(question, Slot(question.Id), null, 1, b => "/media/" + b);

            dto.Display.ContainsKey("scoring").ShouldBeFalse(type);
        }
    }
}
