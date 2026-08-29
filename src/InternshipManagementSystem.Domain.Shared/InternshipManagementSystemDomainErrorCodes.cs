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

    /// <summary>Two forms of one exam sharing a code makes a result untraceable to a paper.</summary>
    public const string ExamFormCodeTaken = "IMS:ExamForm:CodeTaken";

    /// <summary>
    /// A published form cannot be edited: two candidates who sat "Form 2" must
    /// have answered the same paper.
    /// </summary>
    public const string ExamFormNotEditable = "IMS:ExamForm:NotEditable";

    /// <summary>A question this exam cannot draw does not belong on its paper.</summary>
    public const string ExamFormQuestionNotAvailable = "IMS:ExamForm:QuestionNotAvailable";

    /// <summary>
    /// Somebody sat it. Deleting it would leave their result pointing at a paper
    /// that no longer exists.
    /// </summary>
    public const string ExamFormAlreadyUsed = "IMS:ExamForm:AlreadyUsed";

    /// <summary>
    /// Their results reference them. Deleting the person would leave a score
    /// belonging to nobody.
    /// </summary>
    public const string CandidateHasAttempts = "IMS:Candidate:HasAttempts";

    /// <summary>
    /// The address is how a link reaches somebody and how an import recognises
    /// them. Two people sharing one makes both ambiguous.
    /// </summary>
    public const string CandidateEmailTaken = "IMS:Candidate:EmailTaken";

    /// <summary>
    /// A form this tenant does not own, one that no longer exists, or one that
    /// belongs to a different exam than the sitting being sent.
    /// </summary>
    public const string AssignmentFormNotAvailable = "IMS:Assignment:FormNotAvailable";

    /// <summary>
    /// A draft has not been reviewed and a retired form was taken out of rotation
    /// deliberately. Sending either is sending a paper nobody approved.
    /// </summary>
    public const string AssignmentFormNotPublished = "IMS:Assignment:FormNotPublished";

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

    /// <summary>
    /// A graded attempt is somebody's result, and removing one is a
    /// disappearance rather than a correction.
    /// </summary>
    public const string AttemptGradedCannotDelete = "IMS:Attempt:GradedCannotDelete";

    /// <summary>
    /// A published form whose questions have all been deactivated or refiled. A
    /// candidate reaching an empty paper is worse than being told to come back.
    /// </summary>
    public const string ExamFormNoLongerUsable = "IMS:ExamForm:NoLongerUsable";

    // ---- Catalog ----
    public const string CatalogCodeAlreadyExists = "IMS:Catalog:CodeAlreadyExists";

    /// <summary>
    /// Deleting a domain that exams are filed under would unfile them, and an
    /// unfiled exam draws from an empty bank — a paper that silently gets shorter.
    /// </summary>
    public const string CatalogCategoryInUse = "IMS:Catalog:CategoryInUse";

    /// <summary>The same, one rung down.</summary>
    public const string CatalogLevelInUse = "IMS:Catalog:LevelInUse";

    /// <summary>Questions filed under a topic are still about something.</summary>
    public const string CatalogTopicInUse = "IMS:Catalog:TopicInUse";

    /// <summary>
    /// A blueprint rule or an exam section still names this topic. Nothing
    /// enforces a foreign key, and a rule pointing at a deleted topic draws
    /// nothing — so every paper it shapes comes out short, silently.
    /// </summary>
    public const string CatalogTopicInBlueprint = "IMS:Catalog:TopicInBlueprint";

    /// <summary>A topic that is its own ancestor makes a result breakdown loop.</summary>
    public const string CatalogTopicCycle = "IMS:Catalog:TopicCycle";

    // ---- Candidates ----
    public const string CandidateEmailAlreadyExists = "IMS:Candidate:EmailAlreadyExists";

    // ---- Files ----
    public const string FileTooLarge = "IMS:File:TooLarge";
    public const string FileTypeNotAllowed = "IMS:File:TypeNotAllowed";
    public const string FileEmpty = "IMS:File:Empty";
}
