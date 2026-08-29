namespace InternshipManagementSystem.Assessment.Grading.Graders;

/// <summary>
/// Types a machine should not judge. They exist as graders rather than as gaps so
/// the routing stays uniform: every question goes through <c>IGraderResolver</c>,
/// and these ones answer "a human decides".
/// </summary>
public abstract class ManualGraderBase : IQuestionGrader
{
    public abstract string QuestionType { get; }

    public GradeResult Grade(string payload, string? response, decimal maxScore)
        => GradeResult.Manual();
}

/// <summary>Free text. Scored by a reviewer, against a rubric where one is defined.</summary>
public class TextGrader : ManualGraderBase
{
    public override string QuestionType => QuestionTypes.Text;
}

/// <summary>An uploaded document, image or video.</summary>
public class FileUploadGrader : ManualGraderBase
{
    public override string QuestionType => QuestionTypes.FileUpload;
}

/// <summary>A recorded spoken answer. The core of any speaking assessment.</summary>
public class AudioResponseGrader : ManualGraderBase
{
    public override string QuestionType => QuestionTypes.AudioResponse;
}

/// <summary>
/// A point on an agree/disagree scale. There is no right answer to mark, so it
/// carries no marks and is reported rather than scored.
/// </summary>
public class ScaleGrader : IQuestionGrader
{
    public string QuestionType => QuestionTypes.Scale;

    public GradeResult Grade(string payload, string? response, decimal maxScore)
        => new() { AwardedScore = 0m, IsCorrect = null, NeedsManualReview = false };
}
