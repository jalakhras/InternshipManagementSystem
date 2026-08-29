using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternshipManagementSystem.Assessment.Delivery.Dtos;
using InternshipManagementSystem.Assessment.Exams;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Architecture;

/// <summary>
/// Keeps the contract layer from leaking the domain.
/// <para>
/// Lives here rather than beside the other boundary tests because the Domain test
/// project cannot see Contracts — and referencing it from there would be the exact
/// upward dependency this suite exists to forbid. The test has to sit in a layer
/// that can legitimately observe both.
/// </para>
/// </summary>
public class ContractBoundaryTests
{
    private const string Root = "InternshipManagementSystem.Assessment";

    [Fact]
    public void No_contract_exposes_a_domain_entity()
    {
        // A DTO that returns an entity drags the whole model onto the wire, and with
        // it every answer key hanging off that model. This is the structural version
        // of the leak TakerQuestionProjectorTests guards behaviourally: that test
        // proves the current projection is clean, this one stops a future DTO
        // reintroducing the hole by taking the shortcut.
        var contracts = typeof(TakerQuestionDto).Assembly;
        var entities = AssessmentEntities().ToHashSet();

        var violations = new List<string>();

        var dtos = contracts.GetTypes()
            .Where(t => t.IsClass && !t.IsNested)
            .Where(t => t.Namespace?.StartsWith(Root) == true);

        foreach (var dto in dtos)
        {
            foreach (var property in dto.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var candidate in Unwrap(property.PropertyType))
                {
                    if (entities.Contains(candidate))
                    {
                        violations.Add($"{dto.Name}.{property.Name} exposes {candidate.Name}");
                    }
                }
            }
        }

        violations.ShouldBeEmpty(
            "Contracts must not expose domain entities — a DTO carrying an entity " +
            "puts the whole model, answer keys included, on the wire:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void No_taker_facing_contract_carries_an_answer_key()
    {
        // Belt to the projector's braces. The projector decides what a taker sees at
        // runtime; this checks that the *shape* they receive has no field an answer
        // key could ever be assigned to, so the mistake cannot be made one careless
        // property at a time.
        var forbidden = new[] { "payload", "correctanswer", "iscorrect", "explanation", "expectedoutput" };

        var takerTypes = new[]
        {
            typeof(TakerQuestionDto),
            typeof(TakerOptionDto),
            typeof(TakerStimulusDto),
            typeof(ExamPreviewDto),
            typeof(AttemptStateDto),
        };

        var violations = new List<string>();

        foreach (var type in takerTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = property.Name.ToLowerInvariant();

                if (forbidden.Any(f => name.Contains(f)))
                {
                    violations.Add($"{type.Name}.{property.Name}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "These properties sit on a type sent to someone sitting the exam and are " +
            "named like an answer key. The old QuestionDto shipped CorrectAnswer and " +
            "CodeExpectedOutput to the browser, which made every other anti-cheating " +
            "measure decorative:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<Type> AssessmentEntities() =>
        typeof(Exam).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsNested)
            .Where(t => t.Namespace?.StartsWith(Root + ".") == true)
            .Where(t => typeof(Volo.Abp.Domain.Entities.IEntity).IsAssignableFrom(t));

    private static IEnumerable<Type> Unwrap(Type type)
    {
        yield return type;

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var inner in Unwrap(argument))
            {
                yield return inner;
            }
        }
    }
}
