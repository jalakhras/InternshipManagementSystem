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
public enum CandidateStatus : byte
{
    Pending = 0,
    Passed = 1,
    Failed = 2
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

    /// <summary>Developer tools appeared to open. Recorded, never blocked.</summary>
    DevToolsOpened = 4,

    /// <summary>The page was reloaded mid-attempt.</summary>
    PageReloaded = 5
}
