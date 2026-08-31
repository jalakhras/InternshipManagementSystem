using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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

        // The load-bearing assertion, and the one this test did not make. Contracts
        // references Domain.Shared and nothing else, so a DTO property typed as a
        // domain entity does not compile — which meant the scan below ran 78 DTOs
        // and 616 properties against 21 types that could not appear in any of them,
        // and could not have failed however carelessly anybody wrote a DTO. It was
        // restating the compiler.
        //
        // What is worth defending is the reason the compiler can say no: the
        // reference that is not there. Read off the project file rather than off
        // the assembly, because `GetReferencedAssemblies` lists what the compiler
        // actually emitted a reference to — add the ProjectReference alone and it
        // is trimmed away unused, so the assembly cannot tell you the door has been
        // unlocked, only that somebody has already walked through it. The project
        // file can, which is a build earlier and one line to review.
        var references = ProjectReferencesOfContracts();

        references.ShouldNotContain(
            "InternshipManagementSystem.Domain",
            "Application.Contracts must not reference Domain. That reference is the " +
            "only thing that makes it possible to type a DTO property as an entity, " +
            "and the scan below cannot catch anything until it exists — so if it is " +
            "wanted, it is a decision to record, not a convenience to add. " +
            $"Found: {string.Join(", ", references)}");

        references.ShouldNotContain(
            "InternshipManagementSystem.EntityFrameworkCore",
            "Application.Contracts must not reference the persistence layer. " +
            $"Found: {string.Join(", ", references)}");

        var violations = new List<string>();

        var dtos = contracts.GetTypes()
            .Where(t => t.IsClass && !t.IsNested)
            .Where(t => t.Namespace?.StartsWith(Root) == true)
            .ToList();

        // The search space, before anything is concluded from its emptiness. Both
        // sides of the comparison have to be populated for the loop to mean
        // anything, and a namespace rename on either side would silently empty one.
        dtos.Count.ShouldBeGreaterThan(50);
        entities.Count.ShouldBeGreaterThan(15);

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

    /// <summary>
    /// The project references declared by Application.Contracts, by their bare
    /// project name, read from the csproj itself.
    /// <para>
    /// Walks up from the test binary to the directory holding the solution, so it
    /// does not depend on how deep the build output happens to be. If the file
    /// cannot be found the test fails rather than passing over an empty list — an
    /// architecture check that quietly stops reading the thing it checks is the
    /// failure mode this whole file is being repaired for.
    /// </para>
    /// </summary>
    private static List<string> ProjectReferencesOfContracts()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("Could not find the solution directory from " + AppContext.BaseDirectory);

        var csproj = Path.Combine(
            directory!.FullName,
            "src",
            "InternshipManagementSystem.Application.Contracts",
            "InternshipManagementSystem.Application.Contracts.csproj");

        File.Exists(csproj).ShouldBeTrue("Expected the contracts project file at " + csproj);

        return Regex
            .Matches(File.ReadAllText(csproj), @"<ProjectReference\s+Include=""([^""]+)""")
            .Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value))
            .ToList();
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
