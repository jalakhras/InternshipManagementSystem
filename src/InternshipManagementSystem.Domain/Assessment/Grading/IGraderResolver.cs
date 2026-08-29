namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>Finds the grader for a question type, or nothing when none is registered.</summary>
public interface IGraderResolver
{
    /// <summary>Returns the grader for <paramref name="questionType"/>, or null.</summary>
    IQuestionGrader? Resolve(string questionType);
}
