namespace InternshipManagementSystem.Assessment;

/// <summary>How an exam behaves towards the person taking it.</summary>
public enum ExamMode : byte
{
    /// <summary>
    /// A judgement is being made. Correct answers and explanations are never
    /// returned to the taker, and the result is withheld until grading completes.
    /// </summary>
    Assessment = 0,

    /// <summary>
    /// The taker is learning. Correct answers and explanations are revealed after
    /// submission, and the attempt may be repeated.
    /// </summary>
    Practice = 1
}

/// <summary>Relative difficulty, used by the blueprint to build comparable forms.</summary>
public enum QuestionDifficulty : byte
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

/// <summary>Where an exam is in its life cycle.</summary>
public enum ExamStatus : byte
{
    /// <summary>Being written. Cannot be assigned.</summary>
    Draft = 0,

    /// <summary>Ready to assign.</summary>
    Published = 1,

    /// <summary>No longer assignable; existing attempts stay valid.</summary>
    Archived = 2
}

/// <summary>What a candidate's overall evaluation currently says.</summary>
/// <summary>
/// Where a person is in the process, derived and never stored.
/// <para>
/// It used to be Pending/Passed/Failed, defaulted to Pending, and assigned by
/// nothing anywhere — so the candidates screen reported "لم يُدعَ", <i>not
/// invited</i>, about people who had sat the exam and submitted it, and the
/// status filter could only return everything or nothing. The browser meanwhile
/// had its own <c>CandidateStatus</c> with entirely different members; the two
/// shared a name and agreed on nothing.
/// </para>
/// <para>
/// Pass and fail were the wrong idea as well as the wrong data. A candidate
/// sits many exams, so "passed" at the level of a person names no exam and
/// answers no question. What a coordinator scanning a roll actually needs is
/// how far along each person is, which is what this now says.
/// </para>
/// </summary>
public enum CandidateStatus : byte
{
    /// <summary>Nobody has sent them anything yet.</summary>
    Pending = 0,

    /// <summary>Holds a live link and has not opened it.</summary>
    Invited = 1,

    /// <summary>Sitting an exam right now.</summary>
    InProgress = 2,

    /// <summary>Has sat and submitted at least once.</summary>
    Completed = 3,

    /// <summary>
    /// Reserved. Nothing derives it, because nothing in the product can yet
    /// record that somebody withdrew — and inventing it from silence would be
    /// the same mistake this enum was just repaired from.
    /// </summary>
    Withdrawn = 4
}

/// <summary>Why an attempt ended.</summary>
public enum AttemptEndReason : byte
{
    /// <summary>Still running.</summary>
    None = 0,

    /// <summary>The taker pressed submit.</summary>
    SubmittedByCandidate = 1,

    /// <summary>The browser hit zero on the timer and submitted.</summary>
    TimedOutInBrowser = 2,

    /// <summary>The deadline passed and the server submitted on the taker's behalf.</summary>
    TimedOutOnServer = 3,

    /// <summary>An administrator ended it.</summary>
    EndedByAdministrator = 4
}

/// <summary>A behavioural observation during an attempt. Advisory only — see IntegritySignal.</summary>
public enum IntegritySignalType : byte
{
    /// <summary>Text arrived in a single paste event.</summary>
    Paste = 0,

    /// <summary>The exam window lost focus.</summary>
    WindowBlur = 1,

    /// <summary>The answer arrived implausibly fast for its length.</summary>
    ImplausibleSpeed = 2,

    /// <summary>A long answer typed with no corrections.</summary>
    NoCorrections = 3,

    /// <summary>
    /// Developer tools appeared to open.
    /// <para>
    /// <b>Nothing produces this, deliberately.</b> Every way a browser can guess
    /// at it — a sudden gap between window and viewport, a timing difference
    /// around <c>debugger</c> — is a guess, and it is wrong for ordinary things
    /// people do: docking a window, zooming, a screen reader, a slow machine.
    /// </para>
    /// <para>
    /// The whole design of these signals is that a person weighs them, which
    /// only works while they are true. A guess recorded beside real observations
    /// is not a weaker observation; it is a specific false claim about a named
    /// candidate, and the marker has no way to tell it from the others. The
    /// value is kept because attempts already reference these numbers, and
    /// because if a reliable detection ever exists this is where it goes.
    /// </para>
    /// </summary>
    DevToolsOpened = 4,

    /// <summary>The page was reloaded mid-attempt.</summary>
    PageReloaded = 5
}

public enum ExamFormStatus
{
    Draft = 0,
    Published = 1,
    Retired = 2,
}

/// <summary>
/// How an exam decides which questions a particular candidate sees.
/// </summary>
public enum ExamDeliveryMode
{
    /// <summary>
    /// The blueprint draws for each candidate as they start. Nobody reviews the
    /// result and no two candidates sit the same paper. Cheapest to run, and the
    /// only sensible choice for practice.
    /// </summary>
    DrawPerCandidate = 0,

    /// <summary>
    /// Everyone on this exam sits one named form. What a certification body does,
    /// because the paper has to be approved before anyone sees it.
    /// </summary>
    FixedForm = 1,

    /// <summary>
    /// Candidates are spread across the published forms in turn. Keeps the review
    /// guarantee while making a leaked paper worth a fraction of the sitting.
    /// </summary>
    RotateForms = 2,
}
