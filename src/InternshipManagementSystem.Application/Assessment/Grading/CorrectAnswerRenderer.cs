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
                var correct = spec?.Options.Where(o => o.IsCorrect).Select(o => o.Text).ToList();
                return correct is { Count: > 0 } ? string.Join(" • ", correct) : null;
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
}
