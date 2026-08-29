using System.Collections.Generic;
using InternshipManagementSystem.Assessment.Grading.Graders;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Filling in a blank.
/// <para>
/// Written after a review found that a correct answer scored zero. The
/// candidate's screen had no input for this type and fell through to the plain
/// text box, which emits one string; this grader reads a value per blank, could
/// not parse a bare string, and answered <em>wrong</em>. Nobody was ever going
/// to notice, because it did not ask for a person either.
/// </para>
/// </summary>
public class BlanksGraderTests
{
    private readonly FillInTheBlankGrader _grader = new();

    private static readonly string Payload = PayloadJson.Write(new FillInTheBlankPayload
    {
        Blanks =
        [
            new BlankSpec { Id = "b1", AcceptedAnswers = ["went", "travelled"] },
            new BlankSpec { Id = "b2", AcceptedAnswers = ["yesterday"] },
        ],
    });

    [Fact]
    public void Both_blanks_right_is_full_marks()
    {
        var response = PayloadJson.Write(new Dictionary<string, string>
        {
            ["b1"] = "went",
            ["b2"] = "yesterday",
        });

        var result = _grader.Grade(Payload, response, 4m);

        result.AwardedScore.ShouldBe(4m);
        result.NeedsManualReview.ShouldBeFalse();
    }

    [Fact]
    public void A_synonym_the_author_listed_is_right()
    {
        var response = PayloadJson.Write(new Dictionary<string, string>
        {
            ["b1"] = "travelled",
            ["b2"] = "yesterday",
        });

        // The reason a blank holds a list rather than one string: two people can
        // both be right about the same gap.
        _grader.Grade(Payload, response, 4m).AwardedScore.ShouldBe(4m);
    }

    [Fact]
    public void Case_is_ignored_unless_the_author_asked_for_it()
    {
        var response = PayloadJson.Write(new Dictionary<string, string>
        {
            ["b1"] = "  WENT ",
            ["b2"] = "Yesterday",
        });

        // Trimmed and case-insensitive by default. Marking somebody down for a
        // capital letter measures their typing, not their English.
        _grader.Grade(Payload, response, 4m).AwardedScore.ShouldBe(4m);
    }

    [Fact]
    public void One_of_two_earns_half()
    {
        var response = PayloadJson.Write(new Dictionary<string, string>
        {
            ["b1"] = "went",
            ["b2"] = "tomorrow",
        });

        _grader.Grade(Payload, response, 4m).AwardedScore.ShouldBe(2m);
    }

    [Fact]
    public void An_answer_this_grader_cannot_read_goes_to_a_person()
    {
        // A bare string rather than a value per blank — which is exactly what the
        // candidate's screen sent for a while. Calling that wrong took marks from
        // people who had answered correctly, and asked nobody to look.
        var result = _grader.Grade(Payload, "went yesterday", 4m);

        result.NeedsManualReview.ShouldBeTrue();
        result.AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void Nothing_written_is_simply_wrong()
    {
        // Distinct from the case above: an empty answer is a wrong answer, and
        // sending every blank sheet to a marker would bury the queue.
        _grader.Grade(Payload, null, 4m).NeedsManualReview.ShouldBeFalse();
        _grader.Grade(Payload, "   ", 4m).NeedsManualReview.ShouldBeFalse();
    }

    [Fact]
    public void All_or_nothing_when_the_author_says_so()
    {
        var strict = PayloadJson.Write(new FillInTheBlankPayload
        {
            AllowPartialCredit = false,
            Blanks =
            [
                new BlankSpec { Id = "b1", AcceptedAnswers = ["went"] },
                new BlankSpec { Id = "b2", AcceptedAnswers = ["yesterday"] },
            ],
        });

        var response = PayloadJson.Write(new Dictionary<string, string>
        {
            ["b1"] = "went",
            ["b2"] = "tomorrow",
        });

        _grader.Grade(strict, response, 4m).AwardedScore.ShouldBe(0m);
    }
}
