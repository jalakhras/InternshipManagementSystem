using System;
using System.Collections.Generic;
using System.Linq;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Pulls the shufflable item ids out of a payload, without knowing what any of
/// them mean. Used by the form builder to record a per-taker order.
/// </summary>
public static class OptionIdReader
{
    public static List<string> ReadOptionIds(string type, string payload)
    {
        switch (type)
        {
            case QuestionTypes.SingleChoice:
            case QuestionTypes.MultiSelect:
            case QuestionTypes.TrueFalse:
                return PayloadJson.Read<ChoicePayload>(payload)?.Options.Select(o => o.Id).ToList()
                       ?? new List<string>();

            case QuestionTypes.Ordering:
                // The items are presented scrambled; their correct sequence is the answer.
                return PayloadJson.Read<OrderingPayload>(payload)?.Items.Select(i => i.Id).ToList()
                       ?? new List<string>();

            case QuestionTypes.Matching:
                // Only the right-hand column moves; the left column is the prompt.
                return PayloadJson.Read<MatchingPayload>(payload)?.Pairs.Select(p => p.RightId).ToList()
                       ?? new List<string>();

            default:
                return new List<string>();
        }
    }

    /// <summary>
    /// Reorders <paramref name="items"/> to match a stored order, appending anything
    /// the stored order does not mention. Tolerates a bank edited after the form was
    /// frozen rather than failing an in-progress attempt.
    /// </summary>
    public static List<T> ApplyOrder<T>(List<T> items, List<string>? order, Func<T, string> idOf)
    {
        if (order is null || order.Count == 0)
        {
            return items;
        }

        var byId = items.ToDictionary(idOf, StringComparer.OrdinalIgnoreCase);
        var result = new List<T>(items.Count);

        foreach (var id in order)
        {
            if (byId.TryGetValue(id, out var item))
            {
                result.Add(item);
                byId.Remove(id);
            }
        }

        result.AddRange(byId.Values);
        return result;
    }
}
