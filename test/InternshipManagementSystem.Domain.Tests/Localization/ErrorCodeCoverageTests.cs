using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Localization;

/// <summary>
/// Every error code the code can raise has a message a person can read, in both
/// languages.
/// <para>
/// Written because two did not, and the way it surfaced is the point: publishing
/// an empty form showed the author the literal string
/// <c>IMS:ExamForm:NoQuestions</c>. Nothing failed, nothing logged, and the
/// screen looked like it was working. A business review found it by reading, not
/// by using the product — which means it could have shipped.
/// </para>
/// <para>
/// This runs over the source rather than over a registry, so a code added
/// tomorrow is covered without anyone remembering to add it here.
/// </para>
/// </summary>
public class ErrorCodeCoverageTests
{
    [Fact]
    public void Every_error_code_has_a_message_in_both_languages()
    {
        var root = FindRepositoryRoot();
        var codes = ErrorCodesIn(Path.Combine(root, "src"));

        codes.Count.ShouldBeGreaterThan(40, "the scan found suspiciously few codes; check the path");

        foreach (var language in new[] { "en", "ar" })
        {
            var texts = TextsFor(root, language);
            var missing = codes.Where(code => !texts.Contains(code)).OrderBy(c => c).ToList();

            missing.ShouldBeEmpty(
                $"These error codes reach a user as their own key in {language}. " +
                "Whoever sees one is being shown an identifier instead of a reason: " +
                string.Join(", ", missing));
        }
    }

    [Fact]
    public void The_two_languages_carry_the_same_keys()
    {
        var root = FindRepositoryRoot();

        var english = TextsFor(root, "en");
        var arabic = TextsFor(root, "ar");

        // Drift either way is a screen that reads correctly in one language and
        // shows a raw key in the other — and the one nobody on the team reads is
        // the one that stays broken.
        english.Except(arabic).ShouldBeEmpty("present in English, missing in Arabic");
        arabic.Except(english).ShouldBeEmpty("present in Arabic, missing in English");
    }

    private static HashSet<string> ErrorCodesIn(string directory)
    {
        var pattern = new Regex("\"(IMS:[A-Za-z:]+)\"", RegexOptions.Compiled);
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            // Build output holds copies of the sources; scanning them would double
            // the work and report the same codes twice.
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                found.Add(match.Groups[1].Value);
            }
        }

        return found;
    }

    private static HashSet<string> TextsFor(string root, string language)
    {
        var path = Path.Combine(
            root,
            "src",
            "InternshipManagementSystem.Domain.Shared",
            "Localization",
            "InternshipManagementSystem",
            $"{language}.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement
            .GetProperty("texts")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Walks up from the test binary until it finds the solution, so the test does
    /// not depend on where it is run from.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("could not find the solution above the test binary");

        return directory!.FullName;
    }
}
