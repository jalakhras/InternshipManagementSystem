using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Assessment.Exams;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Turns an exam's question bank into one taker's paper.
/// <para>
/// Two takers must get different questions — otherwise a leaked paper is worth
/// something — while still getting <i>comparable</i> papers, or their scores cannot
/// be set side by side. Drawing at random gives the first and loses the second; the
/// blueprint gives both, by drawing a fixed count per topic and difficulty.
/// </para>
/// <para>
/// Everything here is driven by the attempt's stored seed, so the same attempt
/// always rebuilds to the same paper: a reload must not reshuffle under the taker,
/// and a disputed result must be reproducible months later.
/// </para>
/// </summary>
public class ExamFormBuilder : ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;

    public ExamFormBuilder(IGuidGenerator guidGenerator)
    {
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// Selects and orders this attempt's questions.
    /// </summary>
    /// <param name="exam">The exam, with its blueprint loaded.</param>
    /// <param name="bank">Every active question in the exam.</param>
    /// <param name="attemptId">Owner of the resulting rows.</param>
    /// <param name="tenantId">Tenant the attempt belongs to.</param>
    /// <param name="seed">The attempt's persisted shuffle seed.</param>
    public List<AttemptQuestion> Build(
        Exam exam,
        IReadOnlyList<Question> bank,
        Guid attemptId,
        Guid? tenantId,
        int seed)
    {
        if (bank.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamHasNoQuestions);
        }

        var random = new Random(seed);

        var selected = exam.Blueprint.Count > 0
            ? DrawByBlueprint(exam, bank, random)
            : DrawFlat(exam, bank, random);

        if (selected.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamBlueprintUnsatisfiable);
        }

        var ordered = ApplyOrdering(exam, selected, random);

        return ordered
            .Select((question, index) => new AttemptQuestion(
                _guidGenerator.Create(), tenantId, attemptId, question.Id, index, question.Score)
            {
                QuestionGroupId = question.QuestionGroupId,
                // Always ordered for matching and ordering, whatever the exam says.
                //
                // ShuffleOptions is a presentation choice about multiple-choice
                // options. For these two types the stored order is not decoration:
                // the projector builds both sides of a matching question from one
                // list, and only the recorded order pulls them apart. With none,
                // left[i] pairs with right[i] in the JSON a candidate receives —
                // the answer key, handed over on request. An ordering question
                // comes out already in its authored sequence.
                OptionOrder = exam.ShuffleOptions || AlwaysOrdered(question.Type)
                    ? ShuffleOptionIds(question, random)
                    : null
            })
            .ToList();
    }

    /// <summary>
    /// Applies each rule in turn: N questions matching this topic, difficulty and
    /// type. A rule that cannot be filled contributes what it can rather than
    /// failing the whole attempt — a taker mid-exam should not be blocked by an
    /// authoring gap.
    /// </summary>
    private static List<Question> DrawByBlueprint(Exam exam, IReadOnlyList<Question> bank, Random random)
    {
        var taken = new HashSet<Guid>();
        var picked = new List<Question>();

        foreach (var rule in exam.Blueprint.OrderBy(r => r.DisplayOrder))
        {
            var eligible = bank.Where(q =>
                    q.IsActive &&
                    !taken.Contains(q.Id) &&
                    (rule.TopicId is null || q.TopicId == rule.TopicId) &&
                    (rule.Difficulty is null || q.Difficulty == rule.Difficulty) &&
                    (rule.QuestionType is null || string.Equals(q.Type, rule.QuestionType, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var question in Shuffle(eligible, random).Take(rule.QuestionCount))
            {
                picked.Add(question);
                taken.Add(question.Id);
            }
        }

        return picked;
    }

    /// <summary>No blueprint: take the whole bank, or a random subset when the exam caps the form.</summary>
    private static List<Question> DrawFlat(Exam exam, IReadOnlyList<Question> bank, Random random)
    {
        var active = bank.Where(q => q.IsActive).ToList();

        if (exam.QuestionsPerForm is not { } cap || cap >= active.Count)
        {
            return active;
        }

        return Shuffle(active, random).Take(cap).ToList();
    }

    /// <summary>
    /// Orders the paper. Grouped questions are kept together and in their authored
    /// sequence: the questions under a reading passage or a chart follow each other
    /// for a reason, and shuffling across that boundary makes the paper incoherent.
    /// </summary>
    private static List<Question> ApplyOrdering(Exam exam, List<Question> selected, Random random)
    {
        var grouped = selected.Where(q => q.QuestionGroupId.HasValue)
                              .GroupBy(q => q.QuestionGroupId!.Value)
                              .ToList();

        var loose = selected.Where(q => !q.QuestionGroupId.HasValue).ToList();

        // A block is either one group's questions in order, or a single loose question.
        var blocks = new List<List<Question>>();
        blocks.AddRange(grouped.Select(g => g.OrderBy(q => q.DisplayOrder).ToList()));
        blocks.AddRange(loose.Select(q => new List<Question> { q }));

        var orderedBlocks = exam.ShuffleQuestions
            ? Shuffle(blocks, random)
            : blocks.OrderBy(b => b[0].DisplayOrder).ToList();

        return orderedBlocks.SelectMany(b => b).ToList();
    }

    /// <summary>
    /// Records the option order this taker will see, so the same attempt renders
    /// identically on every request and the saved response stays meaningful.
    /// </summary>
    private static string? ShuffleOptionIds(Question question, Random random)
    {
        var ids = Grading.OptionIdReader.ReadOptionIds(question.Type, question.Payload);
        if (ids.Count == 0)
        {
            return null;
        }

        return Grading.PayloadJson.Write(Shuffle(ids, random));
    }

    /// <summary>Fisher-Yates against the attempt's seeded generator, so results are reproducible.</summary>
    private static List<T> Shuffle<T>(IEnumerable<T> source, Random random)
    {
        var items = source.ToList();

        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        return items;
    }

    /// <summary>
    /// Types whose display order carries the answer, so it can never be left to
    /// the authored sequence.
    /// </summary>
    private static bool AlwaysOrdered(string type) =>
        type is QuestionTypes.Matching or QuestionTypes.Ordering;
}
