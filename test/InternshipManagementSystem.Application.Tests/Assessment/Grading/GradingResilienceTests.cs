using System;
using InternshipManagementSystem.Assessment.Grading;
using InternshipManagementSystem.Assessment.Grading.Graders;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// What a candidate can type, and what it must not be able to do.
/// <para>
/// A response is a string the taker chose. Every grader reads one, and a grader
/// that throws on a hostile value does not merely fail that question: the
/// submission rolls back, the attempt cannot be submitted, and the deadline
/// worker force-closes it ungraded and outside every review queue. A candidate
/// who knows they have failed could reach that state deliberately.
/// </para>
/// </summary>
public class GradingResilienceTests
{
    private static string Numeric(decimal correct, decimal tolerance = 0m) =>
        PayloadJson.Write(new NumericPayload { CorrectValue = correct, Tolerance = tolerance });

    [Theory]
    [InlineData("-79228162514264337593543950335")]
    [InlineData("79228162514264337593543950335")]
    public void An_answer_at_the_edge_of_the_decimal_range_is_wrong_rather_than_fatal(string response)
    {
        // decimal throws on overflow where double saturates, so subtracting this
        // from any non-zero key used to raise. An answer that cannot be within the
        // tolerance of anything is simply wrong.
        var result = Should.NotThrow(() => new NumericGrader().Grade(Numeric(1250m, 0.5m), response, 10m));

        result.AwardedScore.ShouldBe(0m);
        result.IsCorrect.ShouldBe(false);
    }

    [Theory]
    [InlineData("not a number")]
    [InlineData("")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e400")]
    [InlineData("٠١٢٣")]
    public void A_response_that_is_not_a_number_is_wrong_rather_than_fatal(string response)
    {
        // Arabic-Indic digits included: the field is typed into by people using an
        // Arabic keyboard, and a parse that throws on them would be triggerable by
        // accident rather than only on purpose.
        var result = Should.NotThrow(() => new NumericGrader().Grade(Numeric(42m), response, 10m));

        result.AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void An_answer_within_the_tolerance_still_scores()
    {
        // The guard must not have narrowed what counts as right.
        new NumericGrader().Grade(Numeric(1250m, 0.5m), "1249.6", 10m).AwardedScore.ShouldBe(10m);
        new NumericGrader().Grade(Numeric(1250m, 0.5m), "1251", 10m).AwardedScore.ShouldBe(0m);
    }

    [Theory]
    [InlineData(QuestionTypes.SingleChoice)]
    [InlineData(QuestionTypes.MultiSelect)]
    [InlineData(QuestionTypes.TrueFalse)]
    [InlineData(QuestionTypes.Numeric)]
    [InlineData(QuestionTypes.Matching)]
    [InlineData(QuestionTypes.Ordering)]
    [InlineData(QuestionTypes.FillInTheBlank)]
    [InlineData(QuestionTypes.Hotspot)]
    [InlineData(QuestionTypes.Code)]
    [InlineData(QuestionTypes.Text)]
    [InlineData(QuestionTypes.FileUpload)]
    [InlineData(QuestionTypes.AudioResponse)]
    [InlineData(QuestionTypes.Scale)]
    public void No_grader_throws_on_a_hostile_response(string type)
    {
        var grader = new GraderResolver(
        [
            new SingleChoiceGrader(), new MultiSelectGrader(), new TrueFalseGrader(),
            new NumericGrader(), new MatchingGrader(), new OrderingGrader(),
            new FillInTheBlankGrader(), new HotspotGrader(), new CodeOutputGrader(), new ScaleGrader(),
            new TextGrader(), new FileUploadGrader(), new AudioResponseGrader(),
        ]).Resolve(type);

        grader.ShouldNotBeNull();

        foreach (var response in new[]
                 {
                     null, "", "   ", "[", "{", "[[[[[", "\"", "null", "[null]",
                     "-79228162514264337593543950335",
                     "{\"__proto__\":{\"x\":1}}",
                     new string('x', 10_000),
                 })
        {
            // The payload is deliberately empty as well: a grader must survive a
            // question it cannot read, not only an answer it cannot read.
            Should.NotThrow(() => grader!.Grade("{}", response, 10m));
        }
    }
}
