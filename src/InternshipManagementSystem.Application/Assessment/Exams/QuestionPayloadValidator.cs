using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Assessment.Grading;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// Checks that a question's payload is something its grader can actually read.
/// <para>
/// The payload is free-form JSON, which is what makes a new question type cost one
/// class instead of a migration. The price is that nothing structural stops an
/// author saving a multiple-choice question with no correct option marked — and
/// without this check, that surfaces as a question nobody can score, discovered
/// while someone is sitting the exam.
/// </para>
/// <para>
/// Each rule below is a mistake that would otherwise reach a candidate.
/// </para>
/// </summary>
public class QuestionPayloadValidator : ITransientDependency
{
    /// <summary>Returns error codes for what is wrong, or empty when the payload is usable.</summary>
    public IReadOnlyList<string> Validate(string type, string payload)
    {
        var errors = new List<string>();

        switch (type)
        {
            case QuestionTypes.SingleChoice:
            case QuestionTypes.TrueFalse:
            {
                var spec = PayloadJson.Read<ChoicePayload>(payload);

                if (spec is null || spec.Options.Count < 2)
                {
                    errors.Add("IMS:Question:NeedsTwoOptions");
                    break;
                }

                var correct = spec.Options.Count(o => o.IsCorrect);

                if (correct == 0)
                {
                    errors.Add("IMS:Question:NoCorrectOption");
                }
                else if (correct > 1)
                {
                    // Would score every taker wrong: the grader requires exactly one.
                    errors.Add("IMS:Question:SingleChoiceHasManyCorrect");
                }

                if (spec.Options.Any(o => string.IsNullOrWhiteSpace(o.Id)))
                {
                    errors.Add("IMS:Question:OptionMissingId");
                }

                if (spec.Options.Select(o => o.Id).Distinct().Count() != spec.Options.Count)
                {
                    // Duplicate ids make a saved answer ambiguous after shuffling.
                    errors.Add("IMS:Question:DuplicateOptionId");
                }

                break;
            }

            case QuestionTypes.MultiSelect:
            {
                var spec = PayloadJson.Read<ChoicePayload>(payload);

                if (spec is null || spec.Options.Count < 2)
                {
                    errors.Add("IMS:Question:NeedsTwoOptions");
                    break;
                }

                if (!spec.Options.Any(o => o.IsCorrect))
                {
                    errors.Add("IMS:Question:NoCorrectOption");
                }

                if (spec.Options.All(o => o.IsCorrect))
                {
                    // Every option correct means selecting everything is right, which
                    // measures nothing.
                    errors.Add("IMS:Question:AllOptionsCorrect");
                }

                break;
            }

            case QuestionTypes.Numeric:
            {
                var spec = PayloadJson.Read<NumericPayload>(payload);

                if (spec is null)
                {
                    errors.Add("IMS:Question:PayloadUnreadable");
                    break;
                }

                if (spec.Tolerance < 0)
                {
                    errors.Add("IMS:Question:NegativeTolerance");
                }

                break;
            }

            case QuestionTypes.Matching:
            {
                var spec = PayloadJson.Read<MatchingPayload>(payload);

                if (spec is null || spec.Pairs.Count < 2)
                {
                    errors.Add("IMS:Question:NeedsTwoPairs");
                    break;
                }

                if (spec.Pairs.Select(p => p.RightId).Distinct().Count() != spec.Pairs.Count)
                {
                    // The right column is shuffled by id; duplicates make the pairing
                    // unrecoverable.
                    errors.Add("IMS:Question:DuplicateOptionId");
                }

                break;
            }

            case QuestionTypes.Ordering:
            {
                var spec = PayloadJson.Read<OrderingPayload>(payload);

                if (spec is null || spec.Items.Count < 2)
                {
                    errors.Add("IMS:Question:NeedsTwoItems");
                    break;
                }

                var positions = spec.Items.Select(i => i.CorrectPosition).OrderBy(p => p).ToList();
                var expected = Enumerable.Range(0, spec.Items.Count).ToList();

                if (!positions.SequenceEqual(expected))
                {
                    // Positions must be a complete 0..n-1 sequence, or partial credit
                    // is computed against a sequence that does not exist.
                    errors.Add("IMS:Question:OrderingPositionsNotSequential");
                }

                break;
            }

            case QuestionTypes.Hotspot:
            {
                var spec = PayloadJson.Read<HotspotPayload>(payload);

                if (spec is null || string.IsNullOrWhiteSpace(spec.ImageBlobName))
                {
                    errors.Add("IMS:Question:HotspotNeedsImage");
                    break;
                }

                if (!spec.Regions.Any(r => r.IsCorrect))
                {
                    errors.Add("IMS:Question:NoCorrectRegion");
                }

                break;
            }

            case QuestionTypes.FillInTheBlank:
            {
                var spec = PayloadJson.Read<FillInTheBlankPayload>(payload);

                if (spec is null || spec.Blanks.Count == 0)
                {
                    errors.Add("IMS:Question:NeedsOneBlank");
                    break;
                }

                if (spec.Blanks.Any(b => b.AcceptedAnswers.Count == 0))
                {
                    errors.Add("IMS:Question:BlankHasNoAnswer");
                }

                break;
            }

            case QuestionTypes.Code:
            {
                var spec = PayloadJson.Read<CodePayload>(payload);

                if (spec is null)
                {
                    errors.Add("IMS:Question:PayloadUnreadable");
                }
                else if (string.IsNullOrWhiteSpace(spec.ExpectedOutput))
                {
                    // Allowed, but it means a human will mark every submission — worth
                    // saying out loud rather than discovering in the review queue.
                    errors.Add("IMS:Question:CodeWithoutExpectedOutputIsManual");
                }

                break;
            }

            case QuestionTypes.Text:
            case QuestionTypes.FileUpload:
            case QuestionTypes.AudioResponse:
            {
                var spec = PayloadJson.Read<RubricPayload>(payload);

                if (spec is not null && spec.Criteria.Count > 0)
                {
                    if (spec.Criteria.Any(c => c.MaxScore <= 0))
                    {
                        errors.Add("IMS:Question:RubricCriterionNeedsScore");
                    }

                    if (spec.Criteria.Select(c => c.Id).Distinct().Count() != spec.Criteria.Count)
                    {
                        errors.Add("IMS:Question:DuplicateRubricCriterion");
                    }
                }

                break;
            }

            case QuestionTypes.Scale:
            {
                var spec = PayloadJson.Read<ScalePayload>(payload);

                if (spec is null || spec.Max <= spec.Min)
                {
                    errors.Add("IMS:Question:ScaleRangeInvalid");
                }

                break;
            }

            default:
                // A type nobody here knows. Not rejected — extensibility is the point
                // of the payload — but the author is told it will be marked by hand.
                errors.Add("IMS:Question:UnknownTypeWillBeManual");
                break;
        }

        return errors;
    }

    /// <summary>
    /// Only the codes that should stop a save. The two informational ones describe
    /// a question that works, just not automatically.
    /// </summary>
    public IReadOnlyList<string> Blocking(string type, string payload) =>
        Validate(type, payload)
            .Where(code => code is not "IMS:Question:CodeWithoutExpectedOutputIsManual"
                               and not "IMS:Question:UnknownTypeWillBeManual")
            .ToList();
}
