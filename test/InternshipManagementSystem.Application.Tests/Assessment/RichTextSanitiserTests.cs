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
}
