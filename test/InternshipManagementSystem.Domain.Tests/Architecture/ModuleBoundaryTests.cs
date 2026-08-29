using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InternshipManagementSystem.Assessment.Exams;
using Shouldly;
using Xunit;

namespace InternshipManagementSystem.Architecture;

/// <summary>
/// Keeps the module boundaries described in docs/architecture/modules.md real.
/// <para>
/// A folder structure without enforcement is a naming convention, and naming
/// conventions drift within a month — usually under deadline, by someone who did
/// not know the line was there. These tests fail the build instead, at the moment
/// the line is crossed, with a message saying which rule and why it exists.
/// </para>
/// <para>
/// Chosen over a full dependency-analysis library on purpose: reflection over our
/// own assemblies is enough for the six rules we care about, and it adds no
/// dependency to a solution that just removed one for a security advisory.
/// </para>
/// </summary>
public class ModuleBoundaryTests
{
    private const string Root = "InternshipManagementSystem.Assessment";

    /// <summary>
    /// Allowed directions. Everything absent from this map is forbidden, so adding
    /// a context means making a deliberate decision about what it may see.
    /// </summary>
    private static readonly Dictionary<string, string[]> AllowedDependencies = new()
    {
        // Knows nothing about anything: it is the tenant's vocabulary.
        ["Catalog"] = [],

        // An exam is filed under a category and a level, and its questions carry topics.
        ["Exams"] = ["Catalog"],

        // A person is filed under a category too.
        ["People"] = ["Catalog"],

        // Delivery hands a specific exam to a specific person.
        ["Delivery"] = ["Exams", "People", "Catalog"],

        // Grading reads the question to score the answer.
        ["Grading"] = ["Delivery", "Exams", "Catalog"],

        // How a tenant appears to its own people. Depends on nothing: branding is
        // read by the shell, the exam page and the certificate, and if it knew
        // about any of them the dependency would run backwards.
        ["Tenancy"] = [],
    };

    private static Assembly DomainAssembly => typeof(Exam).Assembly;

    [Fact]
    public void No_context_depends_on_one_it_should_not_know_about()
    {
        var violations = new List<string>();

        foreach (var type in AssessmentTypes(DomainAssembly))
        {
            var owner = ContextOf(type);
            if (owner is null || !AllowedDependencies.TryGetValue(owner, out var allowed))
            {
                continue;
            }

            foreach (var referenced in ReferencedAssessmentTypes(type))
            {
                var target = ContextOf(referenced);

                if (target is null || target == owner || allowed.Contains(target))
                {
                    continue;
                }

                violations.Add($"{owner}/{type.Name} → {target}/{referenced.Name}");
            }
        }

        violations.ShouldBeEmpty(
            "These references point the wrong way across a module boundary. Dependencies " +
            "run one direction only — a cycle means the two contexts are really one and " +
            "should be merged, not wired together:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations.Distinct()));
    }

    [Fact]
    public void Every_context_folder_is_one_the_architecture_document_names()
    {
        // Catches a seventh context appearing by accident — someone adding a folder
        // rather than deciding to add a context. Grading lives in Application, so
        // the domain side is allowed to be short of it.
        var known = AllowedDependencies.Keys.ToHashSet();

        var found = AssessmentTypes(DomainAssembly)
            .Select(ContextOf)
            .Where(c => c is not null)
            .Distinct()
            .ToList();

        var unknown = found.Where(c => !known.Contains(c!)).ToList();

        unknown.ShouldBeEmpty(
            "These namespaces under Assessment are not contexts the architecture " +
            "document describes. Adding a context is a decision to record there " +
            $"first: {string.Join(", ", unknown)}");
    }

    // ------------------------------------------------------------------ helpers

    private static IEnumerable<Type> AssessmentTypes(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsNested)
            .Where(t => t.Namespace?.StartsWith(Root + ".") == true);

    /// <summary>The context a type belongs to, taken from the segment after Assessment.</summary>
    private static string? ContextOf(Type type)
    {
        var ns = type.Namespace;
        if (ns is null || !ns.StartsWith(Root + "."))
        {
            return null;
        }

        return ns[(Root.Length + 1)..].Split('.')[0];
    }

    /// <summary>
    /// Types this one names in its own surface: property types, constructor
    /// parameters and base type. Deliberately shallow — it is enough to catch a
    /// context reaching for another's entities, which is the mistake in practice.
    /// </summary>
    private static IEnumerable<Type> ReferencedAssessmentTypes(Type type)
    {
        var candidates = new List<Type>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            candidates.AddRange(UnwrapGenerics(property.PropertyType));
        }

        foreach (var constructor in type.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                candidates.AddRange(UnwrapGenerics(parameter.ParameterType));
            }
        }

        if (type.BaseType is not null)
        {
            candidates.AddRange(UnwrapGenerics(type.BaseType));
        }

        return candidates.Where(t => t.Namespace?.StartsWith(Root + ".") == true).Distinct();
    }

    /// <summary>Looks inside List&lt;T&gt;, ICollection&lt;T&gt;, Nullable&lt;T&gt; and the like.</summary>
    private static IEnumerable<Type> UnwrapGenerics(Type type)
    {
        yield return type;

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var inner in UnwrapGenerics(argument))
            {
                yield return inner;
            }
        }
    }
}
