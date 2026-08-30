using System.Text;

namespace InternshipManagementSystem.Assessment;

/// <summary>
/// One spelling for many, when a person's answer is being marked.
/// <para>
/// Arabic writes the same word several ways without changing it: three or four
/// spellings of alef, a final ha that is typed either <c>ة</c> or <c>ه</c>, an
/// alef maqsura for a ya, optional vowel marks nobody types consistently, a
/// tatweel used only to stretch a line, and two sets of digits. None of that is
/// a different answer. Marking the raw characters means «المدرسه» against a key
/// of «المدرسة» scores zero — and the candidate is never told why, because as
/// far as the screen is concerned they simply got it wrong.
/// </para>
/// <para>
/// This is the answer-safe form of the rule. The importer has its own
/// normaliser that also folds punctuation to spaces, which is right for
/// matching a column heading and wrong here: a candidate's answer may contain
/// «3.5» or a hyphenated word, and turning those into spaces would invent a
/// mistake the person did not make. So the two are deliberately separate rather
/// than one shared by both.
/// </para>
/// <para>
/// It does not fold Latin case — the caller decides that, because an author
/// who ticks "case matters" is asking a question about spelling and is entitled
/// to an exact comparison.
/// </para>
/// </summary>
public static class ArabicText
{
    /// <summary>Folds the spellings that do not change the word, and trims.</summary>
    public static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSpace = false;

        foreach (var character in value.Trim())
        {
            // Harakat, tatweel and the superscript alef: decoration, never
            // meaning. Dropped before anything else so a mark between letters
            // cannot break a comparison.
            if (character is 'ـ' or 'ٰ' || (character >= 'ً' && character <= 'ٟ'))
            {
                continue;
            }

            var mapped = character switch
            {
                // Alef, in all its spellings — including the wasla, which no
                // Unicode normalisation form will ever fold for us.
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',

                // The final ha and the alef maqsura, typed either way.
                'ة' => 'ه',
                'ى' => 'ي',

                // Hamza carried on a seat: the seat is the spelling, not the word.
                'ؤ' => 'و',
                'ئ' => 'ي',

                _ => FoldDigit(character),
            };

            if (char.IsWhiteSpace(mapped))
            {
                // Runs of space, and the newline a text box adds, are not part
                // of the answer.
                if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            lastWasSpace = false;
            builder.Append(mapped);
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Arabic-Indic and Eastern Arabic-Indic digits, written as the digits every
    /// parser understands.
    /// <para>
    /// Neither set folds under any Unicode normalisation form, so this has to be
    /// spelled out. A candidate typing «١٢٣» on an Arabic keyboard has answered
    /// one hundred and twenty-three, and a number parser that cannot read it
    /// must not conclude they were wrong.
    /// </para>
    /// </summary>
    public static char FoldDigit(char character) => character switch
    {
        >= '٠' and <= '٩' => (char)('0' + (character - '٠')),
        >= '۰' and <= '۹' => (char)('0' + (character - '۰')),
        _ => character,
    };

    /// <summary>Digits and the separators a number may be written with.</summary>
    public static string FoldNumber(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(character switch
            {
                // The Arabic decimal separator, and the comma half of Europe
                // writes one with.
                '٫' => '.',

                // The Arabic thousands separator, alongside the ordinary one.
                '٬' or ',' or '٬' => '\0',

                _ => FoldDigit(character),
            });
        }

        return builder.Replace("\0", string.Empty).ToString();
    }
}
