using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Assessment.Exams;
using Shouldly;
using Volo.Abp.Guids;
using Xunit;

namespace InternshipManagementSystem.Assessment.Delivery;

/// <summary>
/// Whether laying an exam out in parts changes the paper a candidate is given.
/// <para>
/// It did not. Sections could be named, ordered, given a topic and a count, and
/// the builder had never heard of them: an English exam with Listening, Reading
/// and Grammar drew one shuffled list across all three, so the candidate was
/// asked a listening question, then a grammar one, then a reading one, and the
/// result was a single number for four different things.
/// </para>
/// <para>
/// These tests are about the draw and the order alone — the two properties the
/// builder owns. What the candidate is <i>told</i> about the part they are in,
/// and how the result reports it, are checked where they cross into delivery.
/// </para>
/// </summary>
public class ExamFormBuilderSectionTests
{
    private readonly ExamFormBuilder _builder = new(SimpleGuidGenerator.Instance);

    private static readonly Guid Listening = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Reading = Guid.Parse("11111111-0000-0000-0000-000000000002");
    private static readonly Guid Grammar = Guid.Parse("11111111-0000-0000-0000-000000000003");

    [Fact]
    public void A_sectioned_paper_never_interleaves_its_parts()
    {
        var exam = Exam(
            Section(Listening, "Listening", order: 0),
            Section(Reading, "Reading", order: 1),
            Section(Grammar, "Grammar", order: 2));

        // Shuffled on purpose. Shuffling is what used to scatter a section's
        // questions across the whole paper, and it is on by default.
        exam.ShuffleQuestions = true;

        var bank = Bank((Listening, 4), (Reading, 4), (Grammar, 4));

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 12345);

        // Every part contiguous, and in the order the author put them in. A
        // candidate told "you are in Listening" over a question that came out of
        // Grammar has been told something untrue by the heading.
        SectionRun(paper).ShouldBe(new List<Guid?> { Listening, Reading, Grammar });
    }

    [Fact]
    public void Sections_are_ordered_by_DisplayOrder_not_by_when_they_were_created()
    {
        // Created listening-first, ordered reading-first. The list a repository
        // returns is not the author's order, and the paper has to follow the
        // author's.
        var exam = Exam(
            Section(Listening, "Listening", order: 2),
            Section(Reading, "Reading", order: 0),
            Section(Grammar, "Grammar", order: 1));

        exam.ShuffleQuestions = false;

        var paper = _builder.Build(
            exam, Bank((Listening, 2), (Reading, 2), (Grammar, 2)), Guid.NewGuid(), null, seed: 7);

        SectionRun(paper).ShouldBe(new List<Guid?> { Reading, Grammar, Listening });
    }

    [Fact]
    public void A_section_draws_only_the_number_it_asks_for()
    {
        var exam = Exam(
            Section(Listening, "Listening", order: 0, questionsPerForm: 3),
            Section(Reading, "Reading", order: 1, questionsPerForm: 2));

        var bank = Bank((Listening, 10), (Reading, 10));

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 99);

        // The whole reason QuestionsPerForm exists per section: a bank of ten
        // listening items should not put ten listening questions on every paper.
        paper.Count(q => q.ExamSectionId == Listening).ShouldBe(3);
        paper.Count(q => q.ExamSectionId == Reading).ShouldBe(2);
        paper.Count.ShouldBe(5);
    }

    [Fact]
    public void A_section_with_no_count_serves_everything_it_holds()
    {
        var exam = Exam(Section(Listening, "Listening", order: 0));

        var paper = _builder.Build(exam, Bank((Listening, 6)), Guid.NewGuid(), null, seed: 3);

        // Null is "everything", not "none". Read the other way round this would
        // silently deliver an empty exam.
        paper.Count.ShouldBe(6);
    }

    [Fact]
    public void Two_candidates_draw_different_questions_from_the_same_section()
    {
        var exam = Exam(Section(Listening, "Listening", order: 0, questionsPerForm: 3));
        var bank = Bank((Listening, 12));

        var first = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 1);
        var second = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 2);

        // A per-section draw that always returned the first three would make a
        // leaked paper worth something, which is the reason to draw at all.
        first.Select(q => q.QuestionId).ShouldNotBe(second.Select(q => q.QuestionId).ToList());
    }

    [Fact]
    public void The_same_seed_rebuilds_the_same_sectioned_paper()
    {
        var exam = Exam(
            Section(Listening, "Listening", order: 0, questionsPerForm: 3),
            Section(Reading, "Reading", order: 1, questionsPerForm: 3));

        var bank = Bank((Listening, 8), (Reading, 8));

        var first = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 4242);
        var second = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 4242);

        // A disputed result has to be reproducible months later, and a reload
        // must not reshuffle under the candidate. Sections must not have cost us
        // that.
        first.Select(q => q.QuestionId).ShouldBe(second.Select(q => q.QuestionId).ToList());
    }

    [Fact]
    public void A_section_holding_no_questions_puts_nothing_on_the_paper()
    {
        var exam = Exam(
            Section(Listening, "Listening", order: 0),
            Section(Reading, "Reading", order: 1),
            Section(Grammar, "Grammar", order: 2));

        // Reading was created and never filled — the ordinary state of an exam
        // halfway through being authored.
        var paper = _builder.Build(
            exam, Bank((Listening, 3), (Grammar, 3)), Guid.NewGuid(), null, seed: 5);

        // No slot claims it, so the candidate never sees a heading for a part
        // they are asked nothing about and the result grows no "Reading 0%" row.
        paper.ShouldNotContain(q => q.ExamSectionId == Reading);
        SectionRun(paper).ShouldBe(new List<Guid?> { Listening, Grammar });
    }

    [Fact]
    public void Questions_filed_under_no_section_still_reach_the_paper()
    {
        var exam = Exam(Section(Listening, "Listening", order: 0));

        var bank = Bank((Listening, 3));
        bank.AddRange(Questions(null, 2));

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 11);

        // Adding a first section must not delete the questions an author has not
        // filed yet. They come last, unclaimed by any heading.
        paper.Count.ShouldBe(5);
        paper.Count(q => q.ExamSectionId is null).ShouldBe(2);
        paper.TakeLast(2).ShouldAllBe(q => q.ExamSectionId == null);
    }

    [Fact]
    public void A_passage_is_never_half_drawn()
    {
        var group = Guid.NewGuid();
        var exam = Exam(Section(Reading, "Reading", order: 0, questionsPerForm: 4));

        // One passage with three questions on it, plus five standalone items.
        var bank = Questions(Reading, 3, group);
        bank.AddRange(Questions(Reading, 5));

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 8);

        var fromGroup = paper.Count(q => q.QuestionGroupId == group);

        // Four questions asked for, three of them under one passage: the passage
        // goes whole or not at all. Two of its three is a text the candidate read
        // a third of for nothing, and marks that are not comparable with the
        // candidate who got all three.
        fromGroup.ShouldBeOneOf(0, 3);
        paper.Count.ShouldBe(4);
    }

    [Fact]
    public void A_blueprint_rule_aimed_at_a_section_draws_only_within_it()
    {
        var exam = Exam(
            Section(Listening, "Listening", order: 0),
            Section(Reading, "Reading", order: 1));

        var topic = Guid.NewGuid();

        exam.Blueprint.Add(new ExamBlueprintRule(Guid.NewGuid(), null, exam.Id, questionCount: 2)
        {
            ExamSectionId = Listening,
            TopicId = topic,
        });

        var bank = Bank((Listening, 4), (Reading, 4));

        foreach (var question in bank)
        {
            question.TopicId = topic;
        }

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 6);

        // The rule names a section, so it fills that section and no other. A rule
        // that ignored its own section id would have pulled reading questions in
        // under the listening heading.
        paper.Count(q => q.ExamSectionId == Listening).ShouldBe(2);

        // Reading has no rule of its own, so it serves what it holds.
        paper.Count(q => q.ExamSectionId == Reading).ShouldBe(4);
    }

    [Fact]
    public void An_exam_with_no_sections_is_built_exactly_as_before()
    {
        var exam = Exam();
        exam.ShuffleQuestions = false;

        var bank = Questions(null, 5);

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 1);

        // Most exams are one undivided paper and none of this may change them.
        paper.Count.ShouldBe(5);
        paper.ShouldAllBe(q => q.ExamSectionId == null);
        paper.Select(q => q.Position).ShouldBe(new[] { 0, 1, 2, 3, 4 });
    }

    [Fact]
    public void Every_slot_records_the_section_it_was_served_under()
    {
        var exam = Exam(Section(Listening, "Listening", order: 0));

        var paper = _builder.Build(exam, Bank((Listening, 3)), Guid.NewGuid(), null, seed: 2);

        // Without this column nothing downstream can tell one part of a paper
        // from another, which is what made sections authorable and undeliverable.
        paper.ShouldAllBe(q => q.ExamSectionId == Listening);
    }

    [Fact]
    public void A_sectioned_paper_is_still_the_size_the_exam_asked_for()
    {
        var exam = Exam(Section(Listening, "Listening", 0, questionsPerForm: 2));
        exam.QuestionsPerForm = 4;

        // Two filed, twenty not. Every unfiled question used to be appended,
        // all of them — and the exam's own stated size was the one number this
        // path never read. It was harmless only while filing a question into a
        // section was impossible; the moment that became possible, an author
        // with a bank of two hundred handed their candidate a paper of two
        // hundred.
        var bank = Bank((Listening, 2));
        bank.AddRange(Questions(null, 20));

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 7);

        paper.Count.ShouldBe(4);
    }

    [Fact]
    public void An_exam_that_never_stated_a_size_still_gets_its_whole_bank()
    {
        var exam = Exam(Section(Listening, "Listening", 0, questionsPerForm: 2));

        var bank = Bank((Listening, 2));
        bank.AddRange(Questions(null, 5));

        var paper = _builder.Build(exam, bank, Guid.NewGuid(), null, seed: 7);

        // The half that decides whether the cap is safe to have. An author who
        // never named a size has asked for everything and must keep getting it:
        // dropping unfiled questions on a guess would delete authored content
        // the moment somebody added their first heading, which is not a thing a
        // heading should do.
        paper.Count.ShouldBe(7);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The sections a paper visits, in order, collapsing each run into one entry.</summary>
    private static List<Guid?> SectionRun(List<AttemptQuestion> paper)
    {
        var runs = new List<Guid?>();

        foreach (var slot in paper.OrderBy(q => q.Position))
        {
            if (runs.Count == 0 || runs[^1] != slot.ExamSectionId)
            {
                runs.Add(slot.ExamSectionId);
            }
        }

        return runs;
    }

    private static Exam Exam(params ExamSection[] sections)
    {
        var exam = new Exam(Guid.NewGuid(), null, "Placement", 60);

        foreach (var section in sections)
        {
            exam.Sections.Add(section);
        }

        return exam;
    }

    private static ExamSection Section(Guid id, string name, int order, int? questionsPerForm = null) =>
        new(id, null, Guid.NewGuid(), name, order) { QuestionsPerForm = questionsPerForm };

    private static List<Question> Bank(params (Guid Section, int Count)[] parts)
    {
        var bank = new List<Question>();

        foreach (var part in parts)
        {
            bank.AddRange(Questions(part.Section, part.Count));
        }

        return bank;
    }

    private static List<Question> Questions(Guid? sectionId, int count, Guid? groupId = null) =>
        Enumerable.Range(0, count)
            .Select(i => new Question(Guid.NewGuid(), null, Guid.NewGuid(), QuestionTypes.TrueFalse, "Q" + i)
            {
                ExamSectionId = sectionId,
                QuestionGroupId = groupId,
                DisplayOrder = i,
                Score = 1m,
            })
            .ToList();
}
