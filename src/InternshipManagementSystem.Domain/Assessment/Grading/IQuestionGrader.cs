using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Grades one question type.
/// <para>
/// Replaces the single <c>switch</c> that used to hold every type's rules. Adding a
/// type is now one new class: no edit to any existing grader, no schema change, and
/// each type is unit-testable on its own. A type with no grader registered falls
/// through to manual review rather than silently scoring zero.
/// </para>
/// </summary>
public interface IQuestionGrader : ITransientDependency
{
    /// <summary>The <see cref="QuestionTypes"/> value this grader handles.</summary>
    string QuestionType { get; }

    /// <summary>
    /// Judges <paramref name="response"/> against <paramref name="payload"/>.
    /// </summary>
    /// <param name="payload">The question's stored JSON, including its key.</param>
    /// <param name="response">The taker's stored JSON response. May be null or empty.</param>
    /// <param name="maxScore">Marks this question is worth on this taker's form.</param>
    GradeResult Grade(string payload, string? response, decimal maxScore);
}
