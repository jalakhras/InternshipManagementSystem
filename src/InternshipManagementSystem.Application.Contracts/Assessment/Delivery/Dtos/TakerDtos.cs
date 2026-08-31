using System;
using System.Collections.Generic;

namespace InternshipManagementSystem.Assessment.Delivery.Dtos;

// ---------------------------------------------------------------------------
// Everything in this file crosses to the person sitting the exam.
//
// The previous QuestionDto was the only question DTO in the system and carried
// CorrectAnswer and CodeExpectedOutput, so every answer key was shipped to the
// browser. Blocking developer tools would have been theatre: the keys were in
// the payload.
//
// Nothing here exposes Question.Payload, Question.Explanation (outside Practice,
// after submission), Question.Score weighting internals, or any correctness flag.
// The projection is built server-side from the frozen form, never mapped straight
// off the entity.
// ---------------------------------------------------------------------------

/// <summary>What the taker is told when they open a link, before the clock starts.</summary>
public class ExamPreviewDto
{
    public bool IsAccessible { get; set; }

    /// <summary>Why not, when it is not. Specific: expired, revoked, attempts used up.</summary>
    public string? BlockReason { get; set; }

    /// <summary>
    /// The organisation running this exam, and its mark.
    /// <para>
    /// A candidate opening a placement-test link has no relationship with this
    /// platform and no reason to trust a name they have never heard of. What they
    /// recognise is the centre that told them to expect the email.
    /// </para>
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>Signed, because the person about to see it has no account.</summary>
    public string? OrganizationLogoUrl { get; set; }

    /// <summary>
    /// Where to write if something goes wrong, when the organisation has said.
    /// <para>
    /// A candidate whose connection drops mid-paper, or whose recording will not
    /// start, had nowhere to go: the only address anywhere on their screen was
    /// ours, and they have no relationship with us. The centre invited them and
    /// the centre holds their result.
    /// </para>
    /// </summary>
    public string? OrganizationSupportEmail { get; set; }

    /// <summary>
    /// The organisation's accent colour, so the screen is painted in it.
    /// <para>
    /// Sent to the candidate for the same reason the name and the mark are: they
    /// have no relationship with us and no reason to trust a page in a colour
    /// they have never seen. This is the one screen in the product whose reader
    /// did not choose the platform.
    /// </para>
    /// </summary>
    public string? OrganizationBrandColor { get; set; }

    public string ExamTitle { get; set; } = default!;
    public string? Description { get; set; }
    public string CandidateName { get; set; } = default!;

    public int TimeLimitInMinutes { get; set; }
    public int QuestionCount { get; set; }

    public int AttemptsAllowed { get; set; }
    public int AttemptsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>Practice reveals answers afterwards; Assessment does not.</summary>
    public ExamMode Mode { get; set; }

    /// <summary>Set when an unfinished attempt exists and pressing start will resume it.</summary>
    public Guid? ResumableAttemptId { get; set; }

    /// <summary>Short-lived credential for the exam endpoints. Issued only when accessible.</summary>
    public string? SessionToken { get; set; }
}

/// <summary>A question as the taker sees it. No key, no payload, no explanation.</summary>
public class TakerQuestionDto
{
    public Guid Id { get; set; }

    /// <summary>Position on this taker's paper, zero-based.</summary>
    public int Position { get; set; }

    public int TotalQuestions { get; set; }

    public string Text { get; set; } = default!;
    public string Type { get; set; } = default!;

    /// <summary>Marks available, so the taker can budget effort.</summary>
    public decimal Score { get; set; }

    public int? TimeLimitInSeconds { get; set; }

    /// <summary>Time-limited URL for this question's media, when it has any.</summary>
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }

    /// <summary>The shared stimulus, when this question belongs to a group.</summary>
    public TakerStimulusDto? Stimulus { get; set; }

    /// <summary>
    /// The part of the exam this question sits in, when the exam has parts.
    /// <para>
    /// Null on the exams — most of them — that are one undivided paper. A
    /// candidate who does not know they have moved from reading into listening
    /// does not know the instructions have changed with it.
    /// </para>
    /// </summary>
    public TakerSectionDto? Section { get; set; }

    /// <summary>
    /// Choices with correctness stripped, in this taker's shuffled order.
    /// Empty for types that have no options.
    /// </summary>
    public List<TakerOptionDto> Options { get; set; } = new();

    /// <summary>
    /// Type-specific display data with all answer-bearing fields removed: the items
    /// to order, the left and right columns to match, the blanks to fill, the image
    /// to click, the scale bounds, the starter code.
    /// </summary>
    public Dictionary<string, object?> Display { get; set; } = new();

    /// <summary>What this taker has already saved for this question, so a reload restores it.</summary>
    public string? SavedResponse { get; set; }

    public string? SavedFileName { get; set; }
}

/// <summary>
/// Which part of the exam the candidate is in, and where in it they are.
/// <para>
/// Nothing here is answer-bearing: a name, a position, and instructions written
/// to be read. The section's clock, its floor and its qualifying flag are
/// deliberately absent — none of them is enforced yet, and a candidate told
/// "twenty minutes for this section" by a screen that will not stop them at
/// twenty has been lied to more precisely than by being told nothing.
/// </para>
/// </summary>
public class TakerSectionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>
    /// Sent only on the first question of the section, because that is when it is
    /// true. These are written to be read <i>before</i> a part starts — how many
    /// questions, whether the audio plays once, whether they can go back — and
    /// repeating them over question fourteen is noise a candidate has to read
    /// past under time pressure.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>Where in this section the candidate is, one-based.</summary>
    public int Position { get; set; }

    /// <summary>How many questions this section holds on this candidate's paper.</summary>
    public int QuestionCount { get; set; }

    /// <summary>True on the first question of the section — where the heading is announced.</summary>
    public bool IsFirstQuestion { get; set; }
}

/// <summary>The stimulus a group of questions shares.</summary>
public class TakerStimulusDto
{
    public Guid Id { get; set; }
    public string? Instructions { get; set; }
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
}

/// <summary>One choice. Correctness is deliberately absent.</summary>
public class TakerOptionDto
{
    public string Id { get; set; } = default!;
    public string Text { get; set; } = default!;
    public string? MediaUrl { get; set; }
}

/// <summary>Where the taker is: what is answered, and how long is left.</summary>
public class AttemptStateDto
{
    public Guid AttemptId { get; set; }

    /// <summary>Computed from the stored deadline. The browser clock is never the authority.</summary>
    public int SecondsRemaining { get; set; }

    public int TotalQuestions { get; set; }
    public int AnsweredCount { get; set; }

    /// <summary>Per-position answered flags, for the question map.</summary>
    public List<bool> Answered { get; set; } = new();

    public bool IsSubmitted { get; set; }

    public bool AllowBackNavigation { get; set; }
    public bool OneQuestionAtATime { get; set; }

    /// <summary>
    /// Where to write if something goes wrong, when the organisation has said.
    /// <para>
    /// Carried on the state as well as on the preview, because the moment it is
    /// needed is the moment the preview is long gone: somebody twenty minutes
    /// into a paper whose connection has just dropped, or whose recording will
    /// not start. Reaching it must not depend on having kept the first screen.
    /// </para>
    /// </summary>
    public string? OrganizationSupportEmail { get; set; }

    /// <summary>
    /// The credential for the rest of this sitting. Set by the start; null on the
    /// polls that follow, which are already carrying it.
    /// <para>
    /// The token from the preview screen names no attempt, because at that point
    /// there is none. Every call after the start reads the attempt out of the
    /// token, so the caller has to swap to this one or it is asking about the
    /// empty id — which is what it did, and every question after the start came
    /// back "no such attempt".
    /// </para>
    /// </summary>
    public string? SessionToken { get; set; }
}

/// <summary>An autosave. Sent on navigation and on a short timer while typing.</summary>
public class SaveAnswerDto
{
    public Guid QuestionId { get; set; }

    /// <summary>Response JSON shaped by the question type.</summary>
    public string? Response { get; set; }

    /// <summary>Blob name when the answer is an uploaded file or a recording.</summary>
    public string? AnswerBlobName { get; set; }

    public string? AnswerFileName { get; set; }

    // Behavioural context, collected by the client alongside the answer.
    // Observations about how the text arrived, not judgements about the text.

    public int? TimeSpentSeconds { get; set; }
    public bool WasPasted { get; set; }
    public int KeystrokeCount { get; set; }
    public int BackspaceCount { get; set; }
}

/// <summary>Acknowledgement of a save, carrying the authoritative clock back.</summary>
public class SaveAnswerResultDto
{
    public DateTime SavedAt { get; set; }

    /// <summary>Recomputed server-side on every save, so drift and tampering cannot buy time.</summary>
    public int SecondsRemaining { get; set; }

    /// <summary>True when the deadline passed; the client should stop and submit.</summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Whether the answer was actually written.
    /// <para>
    /// The two flags are not opposites. A save arriving after the deadline
    /// normally stores nothing — and the screen still put a tick beside the word
    /// "Saved", because it read the timestamp and never asked whether anything
    /// had been kept. A candidate was told their answer was safe at the exact
    /// moment it was refused.
    /// </para>
    /// <para>
    /// And a late save can now genuinely store something: a file already on its
    /// way when the clock ran out is accepted inside a short grace, which is
    /// expired and saved at once. So the screen has to be told, not left to
    /// infer.
    /// </para>
    /// </summary>
    public bool Saved { get; set; }
}

/// <summary>What the taker sees once grading has settled.</summary>
public class AttemptResultDto
{
    public Guid AttemptId { get; set; }
    public string ExamTitle { get; set; } = default!;

    /// <summary>False while a human still has answers to mark; the score is withheld until then.</summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// The organisation releases results itself, so this candidate is not shown
    /// one however finished the marking is.
    /// <para>
    /// Distinct from <see cref="IsFinal"/> on purpose. "Somebody is still
    /// marking your answers" and "your result will come from the centre" are
    /// different sentences, and telling a candidate the first when the second is
    /// true leaves them refreshing a page that will never change.
    /// </para>
    /// </summary>
    public bool ScoreWithheld { get; set; }

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal ScorePercentage { get; set; }
    public bool IsPassed { get; set; }

    public DateTime SubmittedAt { get; set; }

    /// <summary>Per-competency breakdown. A single number tells nobody what to do next.</summary>
    public List<TopicScoreDto> TopicBreakdown { get; set; } = new();

    /// <summary>
    /// What the marker wrote for this candidate to read.
    /// <para>
    /// The marking screen labels the box "Feedback for the candidate" and its
    /// hint says, in both languages, that it is shown to them with their result
    /// — "so it is feedback rather than a note to yourself". It was stored on
    /// the answer and carried nowhere: the candidate's result had no field for
    /// it at all. So a marker wrote to somebody who would never read it, and
    /// went on writing, because the screen kept saying they would.
    /// </para>
    /// <para>
    /// Withheld with the score. An organisation that releases results itself is
    /// not releasing half of one, and feedback that arrives before the centre
    /// has seen the mark is the same problem the score setting exists to avoid.
    /// </para>
    /// </summary>
    public List<string> Feedback { get; set; } = new();

    /// <summary>
    /// The same marks read the other way: by the parts the exam was laid out in.
    /// <para>
    /// Alongside the topic breakdown rather than instead of it, because they are
    /// different questions. A topic is what a question measures; a section is
    /// where it sat on the paper — and a candidate who sat "Listening" wants to
    /// see how they did on the thing they remember sitting.
    /// </para>
    /// <para>
    /// In the exam's own order, not worst-first: a result that reorders the parts
    /// of the paper is harder to read back against the paper. Empty on an exam
    /// with no sections, and a section that contributed no questions to this
    /// paper never appears.
    /// </para>
    /// </summary>
    public List<SectionScoreDto> SectionBreakdown { get; set; } = new();

    /// <summary>
    /// Populated only in Practice mode, and only after submission. In Assessment mode
    /// this stays empty — revealing keys would compromise the whole question bank.
    /// </summary>
    public List<PracticeReviewItemDto> Review { get; set; } = new();
}

/// <summary>How the taker did on one competency.</summary>
public class TopicScoreDto
{
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = default!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>How the taker did on one part of the paper.</summary>
public class SectionScoreDto
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = default!;

    /// <summary>How many of this candidate's questions sat in this section.</summary>
    public int QuestionCount { get; set; }

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>Practice-mode feedback on one question: what was right, and why.</summary>
public class PracticeReviewItemDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = default!;
    public string Type { get; set; } = default!;

    public string? YourResponse { get; set; }
    public bool? WasCorrect { get; set; }
    public decimal AwardedScore { get; set; }
    public decimal MaxScore { get; set; }

    /// <summary>The correct answer, rendered for display.</summary>
    public string? CorrectAnswer { get; set; }

    /// <summary>Why it is correct. This is the point of Practice mode.</summary>
    public string? Explanation { get; set; }
}

/// <summary>A behavioural observation reported by the client during an attempt.</summary>
public class ReportIntegritySignalDto
{
    public IntegritySignalType Type { get; set; }
    public Guid? QuestionId { get; set; }

    /// <summary>Characters pasted, seconds away from the window, and so on.</summary>
    public int? Magnitude { get; set; }
}
