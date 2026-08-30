using System.Collections.Generic;
using InternshipManagementSystem.Assessment.Grading.Graders;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Marking what an Arabic keyboard actually writes.
/// <para>
/// The product is Arabic-first, and its two text-marking graders compared raw
/// characters. So «المدرسه» against a key of «المدرسة» scored zero, and «١٢٣»
/// was not merely unread but reported as a <em>wrong</em> number — a mark taken
/// off a person for the keyboard they own. Neither is a different answer, and
/// no Unicode normalisation form folds either one: it has to be written out.
/// </para>
/// </summary>
public class ArabicAnswerTests
{
    private readonly FillInTheBlankGrader _blanks = new();
    private readonly NumericGrader _numeric = new();

    private static string Blank(bool caseSensitive = false) => PayloadJson.Write(
        new FillInTheBlankPayload
        {
            CaseSensitive = caseSensitive,
            Blanks = [new BlankSpec { Id = "b1", AcceptedAnswers = ["المدرسة"] }],
        });

    private static string Typed(string value) =>
        PayloadJson.Write(new Dictionary<string, string> { ["b1"] = value });

    [Theory]
    [InlineData("المدرسه")]    // the final ha, typed the way most people type it
    [InlineData("المدرسة ")]   // a trailing space from the text box
    [InlineData("المَدرسة")]   // a vowel mark, which is decoration
    [InlineData("المدرســة")]  // a tatweel, which only stretches the line
    public void The_same_word_spelled_the_other_way_is_the_same_word(string answer)
    {
        var result = _blanks.Grade(Blank(), Typed(answer), 5m);

        result.AwardedScore.ShouldBe(5m);
    }

    [Fact]
    public void An_author_who_asks_about_spelling_gets_an_exact_comparison()
    {
        // The other half. "Case matters" is a question *about* how a word is
        // written, and folding the spellings underneath it would answer a
        // different question than the one the author set.
        var result = _blanks.Grade(Blank(caseSensitive: true), Typed("المدرسه"), 5m);

        result.AwardedScore.ShouldBe(0m);
    }

    [Fact]
    public void A_genuinely_different_word_is_still_wrong()
    {
        // Normalising must not turn into accepting everything: the point is one
        // spelling for one word, not one mark for any word.
        var result = _blanks.Grade(Blank(), Typed("الجامعة"), 5m);

        result.AwardedScore.ShouldBe(0m);
    }

    [Theory]
    [InlineData("١٢٣")]   // Arabic-Indic
    [InlineData("۱۲۳")]   // Eastern Arabic-Indic
    [InlineData("١٢٣ ")]
    public void Arabic_digits_are_the_number_they_say(string answer)
    {
        var payload = PayloadJson.Write(new NumericPayload { CorrectValue = 123m, Tolerance = 0m });

        var result = _numeric.Grade(payload, answer, 3m);

        result.AwardedScore.ShouldBe(3m);
    }

    [Fact]
    public void The_arabic_decimal_separator_is_a_decimal_point()
    {
        var payload = PayloadJson.Write(new NumericPayload { CorrectValue = 3.5m, Tolerance = 0m });

        var result = _numeric.Grade(payload, "٣٫٥", 3m);

        result.AwardedScore.ShouldBe(3m);
    }

    [Fact]
    public void Something_unreadable_goes_to_a_person_rather_than_being_called_wrong()
    {
        var payload = PayloadJson.Write(new NumericPayload { CorrectValue = 123m, Tolerance = 0m });

        var result = _numeric.Grade(payload, "مئة وثلاثة وعشرون", 3m);

        // Writing the number in words is not a wrong answer. It is an answer
        // this grader cannot read, and the difference is whether a person ever
        // gets to look at it.
        result.NeedsManualReview.ShouldBeTrue();
    }
}
