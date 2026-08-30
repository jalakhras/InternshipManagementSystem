using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace InternshipManagementSystem.Assessment.Exams.Dtos;

/// <summary>
/// A question as its author sees it — payload and all.
/// <para>
/// This is the counterpart to <c>TakerQuestionDto</c>, and the difference between
/// them is the whole security model. This one carries the answer key and is
/// guarded by <c>Questions.View</c>; that one is stripped and goes to anyone with
/// a link. They are separate types rather than one type with conditional fields,
/// because a conditional field is a runtime decision and one day the condition is
/// written wrong.
/// </para>
/// </summary>
public class QuestionDto : AuditedEntityDto<Guid>
{
    /// <summary>Null when the question lives in the shared bank rather than one exam.</summary>
    public Guid? ExamId { get; set; }

    /// <summary>The domain that owns a bank question.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>The level or role a bank question is written for. Null suits every level.</summary>
    public Guid? LevelId { get; set; }

    /// <summary>The part of the exam this question sits in, on a sectioned exam.</summary>
    public Guid? ExamSectionId { get; set; }
    public Guid? QuestionGroupId { get; set; }

    public string Text { get; set; } = default!;
    public string Type { get; set; } = default!;

    /// <summary>Type-specific data, including the key. Never leaves the authoring API.</summary>
    public string Payload { get; set; } = "{}";

    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }

    public QuestionDifficulty Difficulty { get; set; }
    public decimal Score { get; set; }

    public string? Explanation { get; set; }
    public int? TimeLimitInSeconds { get; set; }

    public string? MediaBlobName { get; set; }
    public string? MediaType { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    // Item analysis, accumulated from real attempts.

    public int TimesAnswered { get; set; }

    /// <summary>How many forms have carried this question. High counts mean it has circulated.</summary>
    public int TimesServed { get; set; }

    /// <summary>
    /// Share who answered correctly. Near 1 means the question separates nobody;
    /// near 0 usually means the question or its key is wrong.
    /// </summary>
    public decimal? DifficultyIndex { get; set; }

    /// <summary>
    /// Whether strong candidates outperform weak ones here. At or below zero the
    /// question measures the opposite of what it claims and should be pulled.
    /// </summary>
    public decimal? DiscriminationIndex { get; set; }
}

public class CreateUpdateQuestionDto
{
    /// <summary>The domain to file this question under when it is written into the bank.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>The level a bank question targets. Leave null for one that suits any level.</summary>
    public Guid? LevelId { get; set; }

    /// <summary>
    /// The part of the exam this question belongs to — Listening, Grammar, and so
    /// on. Null on an exam that is not divided into sections, which is most.
    /// </summary>
    public Guid? ExamSectionId { get; set; }
    /// <summary>
    /// The exam that owns this question, or null when it goes into the shared bank.
    /// <para>
    /// Deliberately not <c>[Required]</c>. It was, and the attribute outlived the
    /// day the field became optional — so every attempt to write a bank question
    /// was refused by validation before the service saw it, and the shared bank
    /// could not be created through the API at all. The rule that actually applies
    /// is "belongs to an exam or to a domain", and the service enforces it.
    /// </para>
    /// </summary>
    public Guid? ExamId { get; set; }

    public Guid? QuestionGroupId { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Text { get; set; } = default!;

    /// <summary>One of <see cref="QuestionTypes"/>, or a type added later.</summary>
    [Required]
    [StringLength(64)]
    public string Type { get; set; } = default!;

    /// <summary>
    /// Type-specific JSON. Validated against the type on the server: a payload the
    /// grader cannot read would otherwise surface as a question nobody can score,
    /// discovered mid-exam.
    /// </summary>
    [Required]
    public string Payload { get; set; } = "{}";

    public Guid? TopicId { get; set; }

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

    /// <summary>
    /// Marks for this question. Zero only for a type that carries none.
    /// <para>
    /// The floor was 0.01, which made a scale question impossible to author
    /// correctly: it has no right answer and its grader always awards nothing,
    /// so whatever marks it was forced to carry were added to what every
    /// candidate was measured against and could never be earned back. The type
    /// existed, the picker offered it, and using it quietly cost everybody who
    /// answered it.
    /// </para>
    /// <para>
    /// The floor moves to zero here and the real rule is enforced in the
    /// service, where the question's type is known: exactly zero for a scale
    /// item, more than zero for everything else. An attribute cannot say that.
    /// </para>
    /// </summary>
    [Range(0, 1000)]
    public decimal Score { get; set; } = 1m;

    [StringLength(4000)]
    public string? Explanation { get; set; }

    [Range(5, 7200)]
    public int? TimeLimitInSeconds { get; set; }

    [StringLength(256)]
    public string? MediaBlobName { get; set; }

    [StringLength(32)]
    public string? MediaType { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class QuestionListRequestDto : PagedAndSortedResultRequestDto
{
    public Guid? ExamId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? LevelId { get; set; }

    /// <summary>Restrict to questions in the shared bank, i.e. owned by no exam.</summary>
    public bool? BankOnly { get; set; }

    /// <summary>Restrict to one part of the exam.</summary>
    public Guid? ExamSectionId { get; set; }

    public Guid? TopicId { get; set; }
    public string? Type { get; set; }
    public QuestionDifficulty? Difficulty { get; set; }
    public string? Filter { get; set; }
}

/// <summary>A shared stimulus and the questions hanging off it.</summary>
public class QuestionGroupDto : AuditedEntityDto<Guid>
{
    public Guid ExamId { get; set; }

    public string? Instructions { get; set; }
    public string? StimulusText { get; set; }
    public string? StimulusBlobName { get; set; }
    public string? StimulusMediaType { get; set; }

    public int DisplayOrder { get; set; }

    public List<QuestionDto> Questions { get; set; } = new();
}

public class CreateUpdateQuestionGroupDto
{
    [Required]
    public Guid ExamId { get; set; }

    [StringLength(2048)]
    public string? Instructions { get; set; }

    /// <summary>A reading passage, a scenario, a case. Or use the blob for audio or a chart.</summary>
    public string? StimulusText { get; set; }

    [StringLength(256)]
    public string? StimulusBlobName { get; set; }

    [StringLength(32)]
    public string? StimulusMediaType { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>
/// Describes a question type to the authoring UI.
/// <para>
/// Served from the server rather than hard-coded in Angular so the two cannot
/// disagree about which types exist. A type with no grader registered is reported
/// here as human-graded, which is exactly how it will behave.
/// </para>
/// </summary>
public class QuestionTypeDescriptorDto
{
    public string Type { get; set; } = default!;

    /// <summary>Localisation key for the display name.</summary>
    public string NameKey { get; set; } = default!;

    /// <summary>Localisation key for the one-line explanation of when to use it.</summary>
    public string DescriptionKey { get; set; } = default!;

    /// <summary>Whether a machine can score it, or a person must.</summary>
    public bool IsAutoGraded { get; set; }

    /// <summary>Whether the type presents selectable options that can be shuffled.</summary>
    public bool HasOptions { get; set; }

    /// <summary>Whether the taker uploads a file or records audio.</summary>
    public bool AcceptsUpload { get; set; }

    /// <summary>Bootstrap Icons class for the type picker.</summary>
    public string Icon { get; set; } = default!;
}

// ------------------------------------------------------------------- import

/// <summary>
/// A spreadsheet of questions.
/// <para>
/// The counterpart to the candidate import, and it exists for the same reason:
/// an exam author's question bank is already in a spreadsheet, and retyping
/// eighty questions with four options each is why authoring stops on the first
/// evening.
/// </para>
/// <para>
/// The file is carried as bytes rather than as text so the byte-order mark
/// Excel writes survives the journey and is dealt with in one place. Text would
/// mean whichever client sent it had already decided the encoding, and a client
/// that decided wrongly would produce mojibake nobody could trace.
/// </para>
/// </summary>
public class ImportQuestionsDto
{
    /// <summary>The file exactly as the spreadsheet saved it.</summary>
    [Required]
    public byte[] Content { get; set; } = default!;

    /// <summary>The exam these questions are written into, or null for the shared bank.</summary>
    public Guid? ExamId { get; set; }

    /// <summary>The domain a bank import files under. Required when there is no exam.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>The level a bank import targets. Null suits every level in the domain.</summary>
    public Guid? LevelId { get; set; }

    /// <summary>
    /// Reads the file and reports what would happen without writing anything.
    /// <para>
    /// An author importing eighty rows should see the four that are wrong while
    /// they can still fix the spreadsheet, not afterwards with half a bank
    /// written.
    /// </para>
    /// </summary>
    public bool DryRun { get; set; }
}

public class ImportQuestionsResultDto
{
    public int Created { get; set; }

    /// <summary>
    /// Matched by question text and left alone.
    /// <para>
    /// Importing a corrected sheet a second time must add the six new questions
    /// and not a second copy of the seventy-four that were already there.
    /// </para>
    /// </summary>
    public int AlreadyPresent { get; set; }

    /// <summary>
    /// What the file says, as questions, before anything is written.
    /// <para>
    /// The whole point of the dry run: an author sees the options and the answer
    /// this import read out of their columns, and can tell at a glance that the
    /// key landed on the row they meant.
    /// </para>
    /// </summary>
    public List<ImportQuestionPreviewDto> Preview { get; set; } = new();

    public List<ImportQuestionProblemDto> Problems { get; set; } = new();
}

/// <summary>One row that will become a question, in the words the author will recognise.</summary>
public class ImportQuestionPreviewDto
{
    public int Line { get; set; }

    public string Text { get; set; } = default!;

    /// <summary>The resolved type identifier, so the screen can name it the way the picker does.</summary>
    public string Type { get; set; } = default!;

    public decimal Score { get; set; }

    public QuestionDifficulty Difficulty { get; set; }

    /// <summary>Every option, in the order the columns ran.</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// What this import will mark as right, written out.
    /// <para>
    /// Written out rather than given as numbers, because the mistake worth
    /// catching here is a key that is one row off — and a list of numbers is
    /// exactly as wrong-looking as a correct one.
    /// </para>
    /// </summary>
    public List<string> CorrectAnswers { get; set; } = new();
}

/// <summary>
/// One row that will not become a question, and why.
/// <para>
/// Carries the column as well as the row, because "row 14 is wrong" sends
/// somebody to read nine cells and "the correct answer in row 14 names no
/// option" sends them to one.
/// </para>
/// </summary>
public class ImportQuestionProblemDto
{
    /// <summary>
    /// One-based over the file, so it is the row number the author sees in their
    /// spreadsheet — the headings being row 1.
    /// </summary>
    public int Line { get; set; }

    /// <summary>A localisation key naming the column at fault.</summary>
    public string Column { get; set; } = default!;

    /// <summary>A localisation key, so the reason reads in the reader's language.</summary>
    public string Reason { get; set; } = default!;

    /// <summary>The row as written, so it can be recognised without opening the file.</summary>
    public string Content { get; set; } = default!;
}
