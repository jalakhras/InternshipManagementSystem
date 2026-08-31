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
    /// <param name="exam">The exam, with its blueprint and its sections loaded.</param>
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

        // A sectioned exam is composed part by part, because "twenty questions
        // across four skills" is not the paper "five of each" is. The flat paths
        // stay exactly as they were for the exams — most of them — that have no
        // sections at all.
        var ordered = exam.Sections.Count > 0
            ? DrawBySection(exam, bank, random)
            : OrderBlocks(
                    exam,
                    exam.Blueprint.Count > 0
                        ? DrawByBlueprint(exam, bank, random)
                        : DrawFlat(exam, bank, random),
                    random)
                .Select(q => new PaperSlot(q, q.Score))
                .ToList();

        if (ordered.Count == 0)
        {
            throw new BusinessException(InternshipManagementSystemDomainErrorCodes.ExamBlueprintUnsatisfiable);
        }

        return Project(
            exam,
            ordered,
            attemptId,
            tenantId,
            random);
    }

    /// <summary>
    /// Turns an already-chosen paper into this attempt's rows.
    /// <para>
    /// Split out from <see cref="Build"/> because a named form chooses its own
    /// questions and its own marks and must not be drawn for — but everything after
    /// the choosing is identical, and the one time it was written twice the second
    /// copy forgot the option order and handed out the answer key with it.
    /// </para>
    /// </summary>
    /// <param name="exam">The exam, for its shuffle settings.</param>
    /// <param name="slots">The paper: each question with the marks it carries here.</param>
    /// <param name="attemptId">Owner of the resulting rows.</param>
    /// <param name="tenantId">Tenant the attempt belongs to.</param>
    /// <param name="seed">The attempt's persisted shuffle seed.</param>
    public List<AttemptQuestion> Project(
        Exam exam,
        IReadOnlyList<PaperSlot> slots,
        Guid attemptId,
        Guid? tenantId,
        int seed) =>
        Project(exam, slots, attemptId, tenantId, new Random(seed));

    private List<AttemptQuestion> Project(
        Exam exam,
        IReadOnlyList<PaperSlot> slots,
        Guid attemptId,
        Guid? tenantId,
        Random random)
    {
        return slots
            .Select((slot, index) => new { slot.Question, slot.Score, slot.SectionId, Index = index })
            .Select(row => new AttemptQuestion(
                _guidGenerator.Create(), tenantId, attemptId, row.Question.Id, row.Index, row.Score)
            {
                QuestionGroupId = row.Question.QuestionGroupId,

                // Frozen with the rest of the paper. The candidate is told which
                // part they are in from this, and the result is broken down by it
                // — months later, when the question may have been re-filed or the
                // section deleted.
                // From the section that drew it, falling back to what the
                // question says only for a paper assembled some other way.
                ExamSectionId = row.SectionId ?? row.Question.ExamSectionId,

                // Always ordered for matching and ordering, whatever the exam says.
                //
                // ShuffleOptions is a presentation choice about multiple-choice
                // options. For these two types the stored order is not decoration:
                // the projector builds both sides of a matching question from one
                // list, and only the recorded order pulls them apart. With none,
                // left[i] pairs with right[i] in the JSON a candidate receives —
                // the answer key, handed over on request. An ordering question
                // comes out already in its authored sequence.
                OptionOrder = exam.ShuffleOptions || AlwaysOrdered(row.Question.Type)
                    ? ShuffleOptionIds(row.Question, random)
                    : null
            })
            .ToList();
    }

    /// <summary>
    /// Composes a sectioned paper: each section in turn, in its authored order,
    /// then anything the author never filed under one.
    /// <para>
    /// Shuffling never crosses a section boundary. A listening section whose
    /// questions turn up interleaved with the grammar is not a sectioned exam at
    /// all, and the candidate is told which part they are in — so the parts have
    /// to be contiguous or that heading lies.
    /// </para>
    /// <para>
    /// A section that draws nothing contributes nothing. An empty heading is
    /// worse than an absent one: it tells a candidate a part of the exam exists
    /// and then never asks them anything about it, and it puts a "Listening 0%"
    /// row on a result nobody sat.
    /// </para>
    /// <para>
    /// The exam's own <c>QuestionsPerForm</c> is deliberately not applied here.
    /// Once an exam is divided, each part says how much of itself to draw, and
    /// applying a whole-paper cap on top would cut a section's count back
    /// silently — an author who asked for eight listening questions would get
    /// five and never be told which rule took the other three.
    /// </para>
    /// </summary>
    private static List<PaperSlot> DrawBySection(Exam exam, IReadOnlyList<Question> bank, Random random)
    {
        var taken = new HashSet<Guid>();
        var paper = new List<PaperSlot>();

        // Topics a section has claimed from the bank. What it did not draw is
        // not left over — it was simply not chosen.
        var spokenFor = new HashSet<Guid>();

        foreach (var section in exam.Sections.OrderBy(s => s.DisplayOrder))
        {
            var pool = bank.Where(q => q.IsActive && q.ExamSectionId == section.Id).ToList();

            // Nothing filed here, but the section says what it measures — so it
            // draws from the bank on that.
            //
            // A question in the shared bank belongs to every exam at its level,
            // so it cannot be filed into one exam's section: filing it there
            // would be a claim about a paper it has never seen. Ten comparable
            // products were read before this line was written, and all ten put
            // "which part of the paper" on the structure rather than on the item
            // — as a reference the section holds, or a rule the section owns
            // that selects on what the item already says about itself.
            //
            // This is the smallest form of that. The section's own topic is the
            // rule, and the topic is a fact about the question that is true in
            // every exam. So a language centre can finally say what it has been
            // asking for: draw ten Listening from the bank, and ten Reading.
            if (pool.Count == 0 && section.TopicId is { } topicId)
            {
                pool = bank
                    .Where(q => q.IsActive
                                && q.ExamSectionId == null
                                && q.TopicId == topicId
                                && !taken.Contains(q.Id))
                    .ToList();

                // The section speaks for that topic on this paper, whether it
                // draws two of the twenty or all twenty. Without this the
                // eighteen it did not choose fell through to the unfiled tail
                // and arrived anyway — so a section that said "two Listening"
                // delivered twenty, which is not a section at all.
                spokenFor.Add(topicId);
            }

            if (pool.Count == 0)
            {
                continue;
            }

            var rules = exam.Blueprint.Where(r => r.ExamSectionId == section.Id).ToList();

            // A blueprint rule aimed at this section is more specific than the
            // section's own count and wins: it says which topics and difficulties,
            // where QuestionsPerForm only says how many.
            var picked = rules.Count > 0
                ? DrawByRules(rules, pool, taken, random)
                : Draw(pool, section.QuestionsPerForm, random);

            foreach (var question in picked)
            {
                taken.Add(question.Id);
            }

            paper.AddRange(OrderBlocks(exam, picked, random)
                .Select(q => new PaperSlot(q, q.Score, section.Id)));
        }

        // Questions the author never filed under a section still belong on the
        // paper. Dropping them would delete authored content the moment somebody
        // added their first section, which is not a thing a heading should do.
        var unfiled = bank
            .Where(q => q.IsActive
                        && q.ExamSectionId is null
                        && !taken.Contains(q.Id)
                        && !(q.TopicId is { } t && spokenFor.Contains(t)))
            .ToList();

        var loose = exam.Blueprint.Where(r => r.ExamSectionId is null).ToList();

        List<Question> tail;

        if (loose.Count > 0)
        {
            tail = DrawByRules(loose, unfiled, taken, random);
        }
        else if (exam.QuestionsPerForm is int perForm && perForm > 0)
        {
            // Capped by the number the exam itself asks for, minus what the
            // sections already supplied.
            //
            // Every unfiled question used to be appended, all of them, with no
            // limit. That was harmless only while filing a question into a
            // section was impossible — and the moment it became possible it
            // turned into the first thing a real author would hit: file ten
            // questions into Listening, leave a bank of a hundred and ninety
            // unfiled, and the candidate is handed a paper of a hundred and
            // ninety-five. The exam already carries the number it wants; the
            // sectioned path was the one place that never read it.
            tail = Draw(unfiled, Math.Max(0, perForm - paper.Count), random);
        }
        else
        {
            // No stated size and no rule, so the author has asked for everything
            // and gets it. Dropping unfiled questions on a guess would delete
            // authored content the moment somebody added their first heading,
            // which is not a thing a heading should do.
            tail = unfiled;
        }

        // The unfiled tail belongs to no section, and says so.
        paper.AddRange(OrderBlocks(exam, tail, random).Select(q => new PaperSlot(q, q.Score)));

        return paper;
    }

    /// <summary>
    /// Takes what a section asks for, or everything it holds when it asks for
    /// nothing in particular.
    /// <para>
    /// Drawn by block rather than by question, so a passage is never half-served.
    /// Four of a passage's six questions is a passage the candidate read two
    /// questions' worth of for nothing, and the marks are not comparable with the
    /// candidate who got all six.
    /// </para>
    /// </summary>
    private static List<Question> Draw(List<Question> pool, int? questionsPerForm, Random random)
    {
        if (questionsPerForm is not { } cap || cap >= pool.Count)
        {
            return pool;
        }

        var picked = new List<Question>();

        foreach (var block in Shuffle(BlocksOf(pool), random))
        {
            if (picked.Count + block.Count <= cap)
            {
                picked.AddRange(block);
            }

            if (picked.Count == cap)
            {
                break;
            }
        }

        // Every block on its own is longer than the section wants — one passage of
        // eight questions where the author asked for five. Keeping the passage
        // whole would serve an empty section, which is the worse of two bad
        // answers, so the count wins and the shortfall is visible on the paper.
        return picked.Count > 0 ? picked : Shuffle(pool, random).Take(cap).ToList();
    }

    /// <summary>
    /// Applies each rule in turn: N questions matching this topic, difficulty and
    /// type. A rule that cannot be filled contributes what it can rather than
    /// failing the whole attempt — a taker mid-exam should not be blocked by an
    /// authoring gap.
    /// </summary>
    private static List<Question> DrawByBlueprint(Exam exam, IReadOnlyList<Question> bank, Random random) =>
        DrawByRules(exam.Blueprint.ToList(), bank, new HashSet<Guid>(), random);

    /// <summary>
    /// The blueprint, applied to one pool. Split out because a sectioned exam runs
    /// it once per section, over that section's questions only.
    /// </summary>
    /// <param name="taken">
    /// Shared across sections, so one question cannot be drawn twice onto the same
    /// paper by two rules that both match it.
    /// </param>
    private static List<Question> DrawByRules(
        List<ExamBlueprintRule> rules,
        IReadOnlyList<Question> pool,
        HashSet<Guid> taken,
        Random random)
    {
        var picked = new List<Question>();

        foreach (var rule in rules.OrderBy(r => r.DisplayOrder))
        {
            var eligible = pool.Where(q =>
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
    /// Orders one stretch of the paper. Grouped questions are kept together and in
    /// their authored sequence: the questions under a reading passage or a chart
    /// follow each other for a reason, and shuffling across that boundary makes
    /// the paper incoherent.
    /// <para>
    /// Called once for a flat exam and once per section for a sectioned one, which
    /// is what keeps a shuffle inside its own part of the paper.
    /// </para>
    /// </summary>
    private static List<Question> OrderBlocks(Exam exam, List<Question> selected, Random random)
    {
        var blocks = BlocksOf(selected);

        var orderedBlocks = exam.ShuffleQuestions
            ? Shuffle(blocks, random)
            : blocks.OrderBy(b => b[0].DisplayOrder).ToList();

        return orderedBlocks.SelectMany(b => b).ToList();
    }

    /// <summary>
    /// A block is either one group's questions in their authored sequence, or a
    /// single ungrouped question. Everything that must not be split apart — by a
    /// shuffle or by a draw — is one block.
    /// </summary>
    private static List<List<Question>> BlocksOf(List<Question> selected)
    {
        var grouped = selected.Where(q => q.QuestionGroupId.HasValue)
                              .GroupBy(q => q.QuestionGroupId!.Value)
                              .ToList();

        var loose = selected.Where(q => !q.QuestionGroupId.HasValue).ToList();

        var blocks = new List<List<Question>>();
        blocks.AddRange(grouped.Select(g => g.OrderBy(q => q.DisplayOrder).ToList()));
        blocks.AddRange(loose.Select(q => new List<Question> { q }));

        return blocks;
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

/// <summary>
/// One place on a paper: the question, and what it is worth there.
/// <para>
/// The marks are separate from the question because a named form freezes its own —
/// the same question can be worth two marks on the placement paper and five on the
/// final, and reading them off the question would silently rescore a published
/// form when somebody edited it.
/// </para>
/// </summary>
/// <summary>
/// One place on the paper: the question, what it is worth here, and which
/// section drew it.
/// <para>
/// The section is on the slot and not read off the question, because a question
/// in the shared bank is drawable by every exam at its level and cannot belong
/// to any one paper's part. Which part it landed in is a fact about this paper,
/// not about the question — every comparable product that was read before this
/// was written keeps it on the structure for the same reason.
/// </para>
/// </summary>
public sealed record PaperSlot(Question Question, decimal Score, Guid? SectionId = null);
