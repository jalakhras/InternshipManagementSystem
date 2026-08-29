using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// Reduces authored HTML to the small set of tags a question may carry.
/// <para>
/// The browser runs the same allowlist before sending, and Angular escapes what
/// it renders. This pass exists because neither of those is a control: a request
/// can be made without a browser, and the value stored here is the one every
/// future reader gets — including exports, certificates and any client we have
/// not written yet.
/// </para>
/// <para>
/// An allowlist, never a blocklist. Every published list of "dangerous tags" has
/// been defeated by one its author had not heard of, and the set a question
/// genuinely needs is short enough to write down.
/// </para>
/// </summary>
public static class RichTextSanitiser
{
    /// <summary>Tags a question may contain. Anything else keeps its text and loses its tag.</summary>
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "b", "strong", "i", "em", "u",
        "ul", "ol", "li",
        "p", "br", "div",
        "code", "pre",
        "sub", "sup",
        "span",
    };

    /// <summary>
    /// Tags dropped whole, contents included. Unwrapping a script would leave its
    /// body sitting in the question as visible text — harmless, and nonsense.
    /// </summary>
    private static readonly HashSet<string> Discarded = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "template", "noscript",
    };

    private static readonly Regex TagPattern = new(
        @"<\s*(?<closing>/?)\s*(?<name>[a-zA-Z][a-zA-Z0-9]*)(?<attributes>[^>]*)>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Strips every tag outside the allowlist, and every attribute except
    /// <c>dir</c>.
    /// <para>
    /// Attributes go because almost any of them can be turned against a reader:
    /// an event handler runs code, and <c>style</c> alone can hide the question,
    /// cover the page, or pull a remote image that reports who opened the exam and
    /// when. <c>dir</c> survives because a question may legitimately quote a
    /// passage running the other way.
    /// </para>
    /// </summary>
    public static string Sanitise(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var withoutDiscarded = RemoveDiscardedElements(html);
        var result = new StringBuilder(withoutDiscarded.Length);
        var lastIndex = 0;

        foreach (Match match in TagPattern.Matches(withoutDiscarded))
        {
            result.Append(withoutDiscarded, lastIndex, match.Index - lastIndex);
            lastIndex = match.Index + match.Length;

            var name = match.Groups["name"].Value;

            if (!Allowed.Contains(name))
            {
                // Unwrapped: the tag goes, the text between it and its partner stays.
                continue;
            }

            var isClosing = match.Groups["closing"].Value == "/";

            if (isClosing)
            {
                result.Append("</").Append(name.ToLowerInvariant()).Append('>');
                continue;
            }

            result.Append('<').Append(name.ToLowerInvariant());

            var direction = ReadDirection(match.Groups["attributes"].Value);

            if (direction is not null)
            {
                result.Append(" dir=\"").Append(direction).Append('"');
            }

            // A void element keeps the self-closing form it arrived with, so <br/>
            // does not become <br> and change how a strict parser reads it.
            if (match.Groups["attributes"].Value.TrimEnd().EndsWith('/'))
            {
                result.Append(" /");
            }

            result.Append('>');
        }

        result.Append(withoutDiscarded, lastIndex, withoutDiscarded.Length - lastIndex);

        var sanitised = result.ToString().Trim();

        // What contenteditable leaves behind on an emptied field. Left alone it
        // counts as content everywhere that asks whether a question has text.
        return sanitised == "<br>" || sanitised == "<br />" ? string.Empty : sanitised;
    }

    /// <summary>
    /// Removes a discarded element and everything between its tags. Unterminated
    /// openings are cut to the end, because a script tag with no close is exactly
    /// the shape that survives a naive parser.
    /// </summary>
    private static string RemoveDiscardedElements(string html)
    {
        foreach (var tag in Discarded)
        {
            html = Regex.Replace(
                html,
                $@"<\s*{tag}\b[^>]*>.*?<\s*/\s*{tag}\s*>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            html = Regex.Replace(
                html,
                $@"<\s*/?\s*{tag}\b[^>]*>.*",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        return html;
    }

    /// <summary>Reads a <c>dir</c> attribute, accepting only the two values it has.</summary>
    private static string? ReadDirection(string attributes)
    {
        var match = Regex.Match(
            attributes,
            @"\bdir\s*=\s*[""']?(?<value>rtl|ltr)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value.ToLowerInvariant() : null;
    }
}
