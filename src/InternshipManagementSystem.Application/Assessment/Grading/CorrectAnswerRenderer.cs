using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Renders a question's key as readable text, for Practice-mode feedback after an
/// attempt is over.
/// <para>
/// Kept apart from <c>TakerQuestionProjector</c> on purpose. That class exists to
/// strip keys during an exam; this one exists to reveal them afterwards. Two
/// opposite jobs in one file would be one careless edit away from leaking the bank.
/// </para>
/// </summary>
public static class CorrectAnswerRenderer
{
    public static string? Render(string type, string payload)
    {
        switch (type)
        {
            case QuestionTypes.SingleChoice:
            case QuestionTypes.MultiSelect:
            case QuestionTypes.TrueFalse:
            {
                var spec = PayloadJson.Read<ChoicePayload>(payload);

                if (spec is null)
                {
                    return null;
                }

                if (spec.Weighted == true)
                {
                    return RenderWeighted(spec);
                }

                var correct = spec.Options.Where(o => o.IsCorrect).Select(o => o.Text).ToList();
                return correct.Count > 0 ? string.Join(" • ", correct) : null;
            }

            case QuestionTypes.Numeric:
            {
                var spec = PayloadJson.Read<NumericPayload>(payload);
                if (spec is null)
                {
                    return null;
                }

                var value = spec.CorrectValue.ToString(CultureInfo.InvariantCulture);
                var unit = string.IsNullOrWhiteSpace(spec.Unit) ? string.Empty : " " + spec.Unit;

                return spec.Tolerance > 0
                    ? $"{value}{unit} (± {spec.Tolerance.ToString(CultureInfo.InvariantCulture)})"
                    : $"{value}{unit}";
            }

            case QuestionTypes.Matching:
            {
                var spec = PayloadJson.Read<MatchingPayload>(payload);
                return spec is null
                    ? null
                    : string.Join(" • ", spec.Pairs.Select(p => $"{p.LeftText} → {p.RightText}"));
            }

            case QuestionTypes.Ordering:
            {
                var spec = PayloadJson.Read<OrderingPayload>(payload);
                return spec is null
                    ? null
                    : string.Join(" → ", spec.Items.OrderBy(i => i.CorrectPosition).Select(i => i.Text));
            }

            case QuestionTypes.FillInTheBlank:
            {
                var spec = PayloadJson.Read<FillInTheBlankPayload>(payload);
                return spec is null
                    ? null
                    : string.Join(" • ", spec.Blanks.Select(b => b.AcceptedAnswers.FirstOrDefault() ?? "—"));
            }

            case QuestionTypes.Hotspot:
            {
                var spec = PayloadJson.Read<HotspotPayload>(payload);
                var labels = spec?.Regions.Where(r => r.IsCorrect).Select(r => r.Label ?? "the marked area").ToList();
                return labels is { Count: > 0 } ? string.Join(" • ", labels) : null;
            }

            case QuestionTypes.Code:
            {
                var spec = PayloadJson.Read<CodePayload>(payload);
                return spec?.ExpectedOutput;
            }

            default:
                // Human-graded and unscored types have no single key to show; the
                // reviewer's comment is the feedback instead.
                return null;
        }
    }

    /// <summary>
    /// Renders a weighted question as ranked bands rather than a list of correct
    /// options.
    /// <para>
    /// A reviewer looking at a six-out-of-ten needs to see that the taker picked
    /// something defensible rather than something wrong — otherwise the score is a
    /// number with no account of itself, and the reviewer has to open the payload
    /// to work out what happened.
    /// </para>
    /// <para>
    /// Band labels are English here because this string is composed for a reviewer
    /// screen that renders it verbatim. Localising it means returning the bands as
    /// data instead of a sentence, which is a change to the review DTO rather than
    /// to this renderer — recorded as a follow-up rather than half-done here.
    /// </para>
    /// </summary>
    private static string? RenderWeighted(ChoicePayload spec)
    {
        var bands = new List<string>();

        Add("Best answer", o => o.Weight == 1m);
        Add("Acceptable", o => o.Weight is { } w && w > 0m && w < 1m);
        Add("Not credited", o => (o.Weight ?? 0m) == 0m);
        Add("Penalised", o => o.Weight is { } w && w < 0m);

        return bands.Count > 0 ? string.Join(" • ", bands) : null;

        void Add(string label, Func<OptionPayload, bool> matches)
        {
            var texts = spec.Options.Where(matches).Select(o => o.Text).ToList();

            if (texts.Count > 0)
            {
                bands.Add($"{label}: {string.Join(", ", texts)}");
            }
        }
    }
}
