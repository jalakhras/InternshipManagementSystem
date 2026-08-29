using InternshipManagementSystem.Assessment.Exams;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Assessment;

/// <summary>
/// What survives an authored question, and what does not.
/// <para>
/// The value these tests guard is shown to people sitting an exam, often on
/// their own machines, sometimes hundreds at once. A staff account is trusted;
/// a stolen staff account is the reason this runs on the server rather than
/// only in the browser that submitted the request.
/// </para>
/// </summary>
public class RichTextSanitiserTests
{
    [Theory]
    [InlineData("<b>bold</b>")]
    [InlineData("<em>emphasis</em>")]
    [InlineData("<ul><li>one</li><li>two</li></ul>")]
    [InlineData("<ol><li>first</li></ol>")]
    [InlineData("<pre><code>const x = 1;</code></pre>")]
    [InlineData("<p>H<sub>2</sub>O and x<sup>2</sup></p>")]
    public void Keeps_the_formatting_a_question_actually_needs(string html)
    {
        // Subscripts are not decoration: a chemistry question without them asks
        // something different from the one the author wrote.
        RichTextSanitiser.Sanitise(html).ShouldBe(html);
    }

    [Fact]
    public void Drops_a_script_and_everything_in_it()
    {
        var result = RichTextSanitiser.Sanitise("<p>Before</p><script>steal()</script><p>After</p>");

        result.ShouldNotContain("script");

        // Unwrapping would leave the body behind as visible text — not dangerous,
        // but nonsense sitting in the middle of a question.
        result.ShouldNotContain("steal");
        result.ShouldBe("<p>Before</p><p>After</p>");
    }

    [Fact]
    public void Drops_an_unterminated_script()
    {
        // The shape that survives a parser which only looks for matching pairs.
        RichTextSanitiser.Sanitise("<p>Question</p><script>steal()").ShouldNotContain("steal");
    }

    [Fact]
    public void Removes_event_handlers_while_keeping_the_text()
    {
        var result = RichTextSanitiser.Sanitise("""<p onclick="steal()">Which level is support?</p>""");

        result.ShouldNotContain("onclick");
        result.ShouldNotContain("steal");
        result.ShouldContain("Which level is support?");
    }

    [Fact]
    public void Removes_style_because_a_hidden_question_is_an_unanswerable_one()
    {
        var result = RichTextSanitiser.Sanitise("""<p style="display:none">Prompt</p>""");

        result.ShouldNotContain("style");
        result.ShouldContain("Prompt");
    }

    [Fact]
    public void Unwraps_a_tag_it_does_not_allow_but_keeps_what_it_held()
    {
        // Losing the text would lose the question. Only the tag is the problem.
        RichTextSanitiser.Sanitise("<marquee>Read this</marquee>").ShouldBe("Read this");
        RichTextSanitiser.Sanitise("""<a href="http://elsewhere">link</a>""").ShouldBe("link");
    }

    [Fact]
    public void Keeps_a_direction_attribute_and_only_its_two_real_values()
    {
        // A question may legitimately quote a passage running the other way.
        RichTextSanitiser.Sanitise("""<span dir="ltr">SELECT 1</span>""")
            .ShouldBe("""<span dir="ltr">SELECT 1</span>""");

        RichTextSanitiser.Sanitise("""<span dir="javascript:x">text</span>""")
            .ShouldBe("<span>text</span>");
    }

    [Fact]
    public void Treats_an_empty_editor_as_empty()
    {
        // What contenteditable leaves behind. Left alone it counts as content
        // everywhere that asks whether a question has any text at all.
        RichTextSanitiser.Sanitise("<br>").ShouldBeEmpty();
        RichTextSanitiser.Sanitise("   ").ShouldBeEmpty();
        RichTextSanitiser.Sanitise(null).ShouldBeEmpty();
    }

    [Fact]
    public void Leaves_plain_text_alone()
    {
        const string arabic = "ما مستوى الدعم الأقرب؟";

        RichTextSanitiser.Sanitise(arabic).ShouldBe(arabic);
    }

    // ------------------------------------------------ what a review actually broke

    [Theory]
    [InlineData("<<a>img src=x onerror=alert(1)>")]
    [InlineData("<<a>script>alert(document.domain)<<a>/script>")]
    [InlineData("<<xyz>svg onload=alert(1)>")]
    [InlineData("<b><<a>img src=x onerror=alert(1)></b>")]
    public void A_stray_angle_bracket_cannot_reassemble_into_a_tag(string attack)
    {
        // Every one of these came back out of the old regex sanitiser as a live
        // element — the second as a verbatim script tag. Unwrapping a disallowed
        // tag kept the text on both sides and never rescanned, so the stray "<"
        // joined onto what followed. The fix was not a better pattern; it was
        // parsing the HTML instead of matching it.
        var result = RichTextSanitiser.Sanitise(attack);

        // What matters is that no live element comes out. The parser escapes the
        // stray bracket, so "&lt;img onerror=…&gt;" is the literal text an author
        // typed — it renders as characters and runs nothing. Asserting the absence
        // of the substring "onerror" would be asserting against inert text.
        result.ShouldNotContain("<script", Case.Insensitive);
        result.ShouldNotContain("<img", Case.Insensitive);
        result.ShouldNotContain("<svg", Case.Insensitive);
    }

    [Fact]
    public void A_malformed_discarded_tag_does_not_swallow_the_rest_of_the_question()
    {
        // The old implementation cut from an unterminated discarded tag to the end
        // of the document, silently destroying whatever an author had written after
        // it. Losing a question's second half is not a security bug, but it is a
        // question that goes out wrong.
        var result = RichTextSanitiser.Sanitise("<p>before</p><embed<p>after</p>");

        result.ShouldContain("before");
        result.ShouldContain("after");
    }

    [Theory]
    [InlineData("<p onclick=alert(1)>text</p>")]
    [InlineData("<p ONCLICK='alert(1)'>text</p>")]
    [InlineData("<p onclick\n=alert(1)>text</p>")]
    [InlineData("<div style='position:fixed;inset:0'>text</div>")]
    public void No_attribute_survives_except_direction(string attack)
    {
        var result = RichTextSanitiser.Sanitise(attack);

        result.ShouldNotContain("onclick=", Case.Insensitive);
        result.ShouldNotContain("style=", Case.Insensitive);
        result.ShouldContain("text");
    }

    [Fact]
    public void Nested_and_overlapping_discarded_tags_leave_nothing_executable()
    {
        var result = RichTextSanitiser.Sanitise(
            "<script><script>alert(1)</script></script><style>@import url(x)</style>");

        result.ShouldNotContain("alert");
        result.ShouldNotContain("@import");
    }
}
