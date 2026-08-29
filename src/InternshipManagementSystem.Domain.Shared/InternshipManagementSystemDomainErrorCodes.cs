namespace InternshipManagementSystem;

/// <summary>
/// Business error codes. Each maps to a localised message, so the person on the
/// other end is told what went wrong and what to do — not shown a status code.
/// </summary>
public static class InternshipManagementSystemDomainErrorCodes
{
    // ---- Exam authoring ----
    public const string ExamHasNoQuestions = "IMS:Exam:NoQuestions";
    public const string ExamFormLargerThanBank = "IMS:Exam:FormLargerThanBank";

    /// <summary>A named form cannot be published with nothing on it.</summary>
    public const string ExamFormHasNoQuestions = "IMS:ExamForm:NoQuestions";

    /// <summary>
    /// The same question twice on one paper. It is scored twice and read twice,
    /// and the taker reasonably concludes the exam is broken.
    /// </summary>
    public const string ExamFormHasDuplicateQuestions = "IMS:ExamForm:DuplicateQuestions";

    /// <summary>
    /// A question was submitted with neither an owning exam nor a domain to file it
    /// under, which would leave it invisible to both.
    /// </summary>
    public const string QuestionBelongsNowhere = "IMS:Question:BelongsNowhere";
    public const string ExamNotPublished = "IMS:Exam:NotPublished";
    public const string ExamOutsideSchedule = "IMS:Exam:OutsideSchedule";
    public const string ExamBlueprintUnsatisfiable = "IMS:Exam:BlueprintUnsatisfiable";

    // ---- Exam links ----
    public const string ExamLinkInvalid = "IMS:ExamLink:Invalid";
    public const string ExamLinkExpired = "IMS:ExamLink:Expired";
    public const string ExamLinkRevoked = "IMS:ExamLink:Revoked";
    public const string ExamLinkAttemptsExhausted = "IMS:ExamLink:AttemptsExhausted";

    // ---- Attempts ----
    public const string AttemptAlreadySubmitted = "IMS:Attempt:AlreadySubmitted";
    public const string AttemptExpired = "IMS:Attempt:Expired";
    public const string AttemptNotFound = "IMS:Attempt:NotFound";
    public const string AttemptNotSubmitted = "IMS:Attempt:NotSubmitted";
    public const string AttemptStillAwaitingReview = "IMS:Attempt:AwaitingReview";
    public const string AttemptQuestionNotOnForm = "IMS:Attempt:QuestionNotOnForm";

    // ---- Assignment ----
    public const string AssignmentTargetMissing = "IMS:Assignment:TargetMissing";
    public const string AssignmentTargetAmbiguous = "IMS:Assignment:TargetAmbiguous";
    public const string AssignmentExpiryInPast = "IMS:Assignment:ExpiryInPast";
    public const string AssignmentGroupEmpty = "IMS:Assignment:GroupEmpty";

    // ---- Catalog ----
    public const string CatalogCodeAlreadyExists = "IMS:Catalog:CodeAlreadyExists";

    // ---- Candidates ----
    public const string CandidateEmailAlreadyExists = "IMS:Candidate:EmailAlreadyExists";

    // ---- Files ----
    public const string FileTooLarge = "IMS:File:TooLarge";
    public const string FileTypeNotAllowed = "IMS:File:TypeNotAllowed";
    public const string FileEmpty = "IMS:File:Empty";
}
