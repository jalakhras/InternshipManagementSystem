using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using Ganss.Xss;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// Reduces authored HTML to the small set of tags a question may carry.
/// <para>
/// The browser runs the same allowlist before sending, and Angular escapes what
/// it renders. This pass exists because neither of those is a control: a request
/// can be made without a browser, and the value stored here is the one every
/// future reader gets — exports, certificates, and any client not yet written.
/// </para>
/// <para>
/// <b>This was a regular expression, and a security review defeated it in one
/// line.</b> Unwrapping a disallowed tag kept the text on both sides and never
/// rescanned, so a stray <c>&lt;</c> joined onto what followed and reassembled
/// into a live tag: <c>&lt;&lt;a&gt;script&gt;…</c> came out of the allowlist as
/// a verbatim script element. Every patch to that approach reopened it somewhere
/// else, because the flaw was the approach. HTML is parsed here now, by a
/// library that exists for this and is maintained by people who watch for the
/// next such trick.
/// </para>
/// </summary>
public static class RichTextSanitiser
{
    /// <summary>
    /// Configured once. The sanitiser is thread-safe for reading and this type is
    /// static, so a per-call instance would be work repeated on every save.
    /// </summary>
    private static readonly HtmlSanitizer Sanitizer = Configure();

    /// <summary>
    /// Tags whose contents go with them rather than being unwrapped into the text.
    /// </summary>
    private static readonly HashSet<string> DiscardedWholesale = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "template", "noscript", "svg", "math",
    };

    private static HtmlSanitizer Configure()
    {
        var sanitiser = new HtmlSanitizer();

        // Started from empty rather than trimmed from the library's default set.
        // A default that changes in a future version would silently widen what a
        // question may carry, and nothing in this file would say so.
        sanitiser.AllowedTags.Clear();
        sanitiser.AllowedAttributes.Clear();
        sanitiser.AllowedCssProperties.Clear();
        sanitiser.AllowedSchemes.Clear();

        foreach (var tag in new[]
                 {
                     "b", "strong", "i", "em", "u",
                     "ul", "ol", "li",
                     "p", "br", "div",
                     "code", "pre",
                     "sub", "sup",
                     "span",
                 })
        {
            sanitiser.AllowedTags.Add(tag);
        }

        // Almost nothing survives. An event handler runs code; `style` alone can
        // hide the question, cover the page, or pull a remote image that reports
        // who opened the exam and when. `dir` stays because a question may
        // legitimately quote a passage running the other way.
        sanitiser.AllowedAttributes.Add("dir");

        // No links and no images: a question's media has its own column, and a
        // link in a prompt is a way out of the exam.
        //
        // Children are kept when a tag is dropped, so an unknown wrapper loses the
        // tag and not the question's text.
        sanitiser.KeepChildNodes = true;

        // Except for these. Keeping a script's children means keeping its source
        // as visible text in the middle of a question — inert, since the parser
        // escapes it, but nonsense that an author never wrote and cannot see to
        // remove. Nothing inside them was ever content.
        sanitiser.RemovingTag += (_, e) =>
        {
            if (DiscardedWholesale.Contains(e.Tag.NodeName))
            {
                e.Tag.InnerHtml = string.Empty;
            }
        };

        // dir survives the allowlist but its value is not checked by it, and a
        // direction that is not a direction is either a typo or someone probing.
        // Not dangerous — a browser ignores it — but a question should not carry
        // attributes nobody meant to write.
        sanitiser.PostProcessNode += (_, e) =>
        {
            if (e.Node is not IElement element)
            {
                return;
            }

            var direction = element.GetAttribute("dir");

            if (direction is not null && direction is not "ltr" and not "rtl")
            {
                element.RemoveAttribute("dir");
            }
        };

        return sanitiser;
    }

    /// <summary>
    /// Strips every tag outside the allowlist, keeping the text those tags held,
    /// and drops script-like elements along with their contents.
    /// </summary>
    public static string Sanitise(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitised = Sanitizer.Sanitize(html).Trim();

        // What contenteditable leaves behind on an emptied field. Left alone it
        // counts as content everywhere that asks whether a question has text.
        return sanitised is "<br>" or "<br />" ? string.Empty : sanitised;
    }
}
