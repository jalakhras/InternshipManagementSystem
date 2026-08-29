using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace InternshipManagementSystem.Assessment.Grading;

/// <summary>
/// Maps a question type to its grader.
/// <para>
/// Every <see cref="IQuestionGrader"/> in the container is picked up automatically,
/// so a new type is registered by existing itself. An unknown type resolves to
/// nothing and the caller routes the answer to manual review — which means adding a
/// type can never silently score people zero.
/// </para>
/// </summary>
public class GraderResolver : IGraderResolver, ISingletonDependency
{
    private readonly Dictionary<string, IQuestionGrader> _byType;

    public GraderResolver(IEnumerable<IQuestionGrader> graders)
    {
        _byType = graders
            .GroupBy(g => g.QuestionType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public IQuestionGrader? Resolve(string questionType)
    {
        if (string.IsNullOrWhiteSpace(questionType))
        {
            return null;
        }

        return _byType.TryGetValue(questionType, out var grader) ? grader : null;
    }
}
