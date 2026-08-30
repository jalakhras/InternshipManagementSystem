using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using InternshipManagementSystem.Assessment.Grading;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Builds what a taker is allowed to see.
/// <para>
/// This is the only path from a <see cref="Question"/> to the wire during an exam.
/// Nothing else may map a question entity to a response: keeping it in one place is
/// what makes "the answer key never leaves the server" a property you can check by
/// reading a single file, rather than a convention every future endpoint has to
/// remember.
/// </para>
/// <para>
/// Each type is stripped explicitly. An unrecognised type yields no display data at
/// all — failing closed, so a type added later cannot leak its key by default.
/// </para>
/// </summary>
public class TakerQuestionProjector : ITransientDependency
{
    /// <summary>
    /// Projects one question onto its slot on the taker's paper.
    /// </summary>
    /// <param name="question">The bank entity. Its payload does not cross the wire.</param>
    /// <param name="slot">This taker's frozen position and option order.</param>
    /// <param name="group">The shared stimulus, when the question has one.</param>
    /// <param name="totalQuestions">Length of this taker's paper.</param>
    /// <param name="mediaUrlFactory">Turns a blob name into a time-limited URL.</param>
    /// <param name="section">Where this question sits among the exam's parts, when it has any.</param>
    public TakerQuestionDto Project(
        Question question,
        AttemptQuestion slot,
        QuestionGroup? group,
        int totalQuestions,
        Func<string, string> mediaUrlFactory,
        SectionPlacement? section = null)
    {
        var dto = new TakerQuestionDto
        {
            Id = question.Id,
            Position = slot.Position,
            TotalQuestions = totalQuestions,
            Text = question.Text,
            Type = question.Type,
            Score = slot.Score,
            TimeLimitInSeconds = question.TimeLimitInSeconds,
            MediaType = question.MediaType,
            MediaUrl = question.MediaBlobName is null ? null : mediaUrlFactory(question.MediaBlobName)
        };

        if (group is not null)
        {
            dto.Stimulus = new TakerStimulusDto
            {
                Id = group.Id,
                Instructions = group.Instructions,
                Text = group.StimulusText,
                MediaType = group.StimulusMediaType,
                MediaUrl = group.StimulusBlobName is null ? null : mediaUrlFactory(group.StimulusBlobName)
            };
        }

        if (section is not null)
        {
            dto.Section = new TakerSectionDto
            {
                Id = section.Section.Id,
                Name = section.Section.Name,

                // Only where they are true. A section's instructions are written to
                // be read before it starts — "you will hear the recording once" —
                // and repeating them over question fourteen is something a
                // candidate has to read past under time pressure.
                Instructions = section.Position == 1 ? section.Section.Instructions : null,

                Position = section.Position,
                QuestionCount = section.QuestionCount,
                IsFirstQuestion = section.Position == 1
            };

            // Deliberately not sent: TimeLimitInMinutes, MinimumPercentage and
            // IsQualifying. Nothing enforces any of the three yet, and a candidate
            // shown "20 minutes for this part" by a screen that will not stop them
            // at twenty has been misled more precisely than by being told nothing.
        }

        var savedOrder = PayloadJson.Read<List<string>>(slot.OptionOrder);

        switch (question.Type)
        {
            case QuestionTypes.SingleChoice:
            case QuestionTypes.MultiSelect:
            case QuestionTypes.TrueFalse:
            {
                var spec = PayloadJson.Read<ChoicePayload>(question.Payload);
                if (spec is not null)
                {
                    // IsCorrect is dropped here. This single omission is what the
                    // old QuestionDto got wrong.
                    var options = spec.Options
                        .Select(o => new TakerOptionDto
                        {
                            Id = o.Id,
                            Text = o.Text,
                            MediaUrl = o.BlobName is null ? null : mediaUrlFactory(o.BlobName)
                        })
                        .ToList();

                    dto.Options = OptionIdReader.ApplyOrder(options, savedOrder, o => o.Id);
                }

                break;
            }

            case QuestionTypes.Ordering:
            {
                var spec = PayloadJson.Read<OrderingPayload>(question.Payload);
                if (spec is not null)
                {
                    // CorrectPosition is the answer, so only id and text travel.
                    var items = spec.Items
                        .Select(i => new { id = i.Id, text = i.Text })
                        .ToList();

                    dto.Display["items"] = OptionIdReader.ApplyOrder(items, savedOrder, i => i.id);
                }

                break;
            }

            case QuestionTypes.Matching:
            {
                var spec = PayloadJson.Read<MatchingPayload>(question.Payload);
                if (spec is not null)
                {
                    // The two columns are sent separately so the pairing is not implied
                    // by their order.
                    dto.Display["left"] = spec.Pairs
                        .Select(p => new { id = p.LeftId, text = p.LeftText })
                        .ToList();

                    var right = spec.Pairs
                        .Select(p => new { id = p.RightId, text = p.RightText })
                        .ToList();

                    dto.Display["right"] = OptionIdReader.ApplyOrder(right, savedOrder, r => r.id);
                }

                break;
            }

            case QuestionTypes.Numeric:
            {
                var spec = PayloadJson.Read<NumericPayload>(question.Payload);
                if (spec is not null)
                {
                    // The unit is a display hint. The value and its tolerance are the answer.
                    dto.Display["unit"] = spec.Unit;
                }

                break;
            }

            case QuestionTypes.Hotspot:
            {
                var spec = PayloadJson.Read<HotspotPayload>(question.Payload);
                if (spec is not null)
                {
                    // The image only. Sending regions would draw the answer on the screen.
                    dto.Display["imageUrl"] = mediaUrlFactory(spec.ImageBlobName);
                }

                break;
            }

            case QuestionTypes.FillInTheBlank:
            {
                var spec = PayloadJson.Read<FillInTheBlankPayload>(question.Payload);
                if (spec is not null)
                {
                    // Which blanks exist, never what goes in them.
                    dto.Display["blankIds"] = spec.Blanks.Select(b => b.Id).ToList();
                }

                break;
            }

            case QuestionTypes.Code:
            {
                var spec = PayloadJson.Read<CodePayload>(question.Payload);
                if (spec is not null)
                {
                    dto.Display["language"] = spec.Language;
                    dto.Display["starterTemplate"] = spec.StarterTemplate;
                    // ExpectedOutput is the answer and stays behind.
                }

                break;
            }

            case QuestionTypes.Scale:
            {
                var spec = PayloadJson.Read<ScalePayload>(question.Payload);
                if (spec is not null)
                {
                    dto.Display["min"] = spec.Min;
                    dto.Display["max"] = spec.Max;
                    dto.Display["minLabel"] = spec.MinLabel;
                    dto.Display["maxLabel"] = spec.MaxLabel;
                }

                break;
            }

            case QuestionTypes.Text:
            case QuestionTypes.FileUpload:
            case QuestionTypes.AudioResponse:
            {
                var spec = PayloadJson.Read<RubricPayload>(question.Payload);
                if (spec is not null)
                {
                    // Criterion names and weights help the taker aim; ReviewerGuidance
                    // is written for the marker and stays server-side.
                    dto.Display["criteria"] = spec.Criteria
                        .Select(c => new { name = c.Name, maxScore = c.MaxScore })
                        .ToList();
                }

                break;
            }

            default:
                // Unknown type: send the prompt and nothing else. A new type has to
                // opt in to exposing anything.
                break;
        }

        return dto;
    }
}

/// <summary>
/// One question's place among the exam's parts: the section, and how far into it
/// the candidate is.
/// <para>
/// Computed against the frozen paper rather than against the authored exam. Two
/// candidates drawing different numbers of listening questions are each "3 of 6"
/// or "3 of 8" according to the paper in front of them, and a section the draw
/// never reached does not exist for them at all.
/// </para>
/// </summary>
/// <param name="Section">The part of the exam, for its name and its instructions.</param>
/// <param name="Position">Where in the section this question is, one-based.</param>
/// <param name="QuestionCount">How many questions the section holds on this paper.</param>
public sealed record SectionPlacement(ExamSection Section, int Position, int QuestionCount);
