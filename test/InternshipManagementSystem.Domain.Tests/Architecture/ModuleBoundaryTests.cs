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
        var crossings = new List<string>();
        var violations = new List<string>();

        foreach (var type in AssessmentTypes(DomainAssembly))
        {
            var owner = ContextOf(type);
            if (owner is null || !AllowedDependencies.TryGetValue(owner, out var allowed))
            {
                continue;
            }

            foreach (var (referenced, member) in LinksFrom(type))
            {
                var target = ContextOf(referenced);

                if (target is null || target == owner)
                {
                    continue;
                }

                var link = $"{owner}/{type.Name}.{member} → {target}/{referenced.Name}";

                crossings.Add(link);

                if (allowed.Contains(target))
                {
                    continue;
                }

                violations.Add(link);
            }
        }

        // The half without which the rest is decoration. This test spent its whole
        // life green over an empty search space: it looked only at property types,
        // constructor parameters and base types, and there is not one typed
        // cross-context reference in this domain — no EF navigation property
        // crosses a context. Every relationship here is a bare Guid, whose
        // namespace is System, so `target == owner` short-circuited on every
        // iteration and AllowedDependencies was consulted twenty-two times without
        // ever rejecting anything. Adding Catalog.Topic.ExamId — a textbook
        // violation, since Catalog is allowed to know nothing — left all ten tests
        // in this project green.
        //
        // So: prove the detector can see, before believing what it did not find.
        crossings.Count.ShouldBeGreaterThan(
            12,
            "The boundary detector found almost no cross-context links, which in this " +
            "domain means it has stopped being able to see them rather than that they " +
            "are gone. Check that Guid foreign keys still resolve to entity names " +
            "before trusting the emptiness below.");

        // And that it resolves the two directions the architecture actually turns
        // on, rather than twelve incidental ones.
        crossings.ShouldContain("Delivery/Attempt.ExamId → Exams/Exam");
        crossings.ShouldContain("Exams/Question.TopicId → Catalog/Topic");

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

    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Every entity in the assessment domain, by its simple name. Names that are
    /// not unique are dropped: an ambiguous name resolves nothing and a guess
    /// would be worse than a miss.
    /// </summary>
    private static readonly Lazy<Dictionary<string, Type>> EntitiesByName = new(() =>
        AssessmentTypes(DomainAssembly)
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single(), StringComparer.Ordinal));

    /// <summary>
    /// Everything this type names across a boundary, and the member that names it.
    /// <para>
    /// Two kinds, because this domain uses both. A typed reference — a property, a
    /// constructor parameter, a base type — is the obvious one and the one the
    /// detector used to look for exclusively. But there is not a single typed
    /// cross-context reference in this assembly: relationships are spelled as
    /// <c>Guid</c> foreign keys, and a <c>Guid</c> has no context. So the second
    /// kind is the naming convention those keys follow — <c>ExamId</c> names
    /// <c>Exam</c> — which is how a boundary is actually crossed here and
    /// therefore the only way this test can catch one being crossed wrongly.
    /// </para>
    /// <para>
    /// A key whose prefix names no entity is skipped rather than guessed at:
    /// <c>TenantId</c>, <c>UserId</c> and <c>ParentId</c> all fall out that way,
    /// and so does <c>FixedFormId</c>, which is a miss the convention cannot
    /// recover — a foreign key that does not say what it points at is invisible
    /// here, and renaming it is the fix.
    /// </para>
    /// </summary>
    private static IEnumerable<(Type Target, string Member)> LinksFrom(Type type)
    {
        foreach (var referenced in ReferencedAssessmentTypes(type))
        {
            yield return (referenced, "<typed>");
        }

        foreach (var property in type.GetProperties(Declared))
        {
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (propertyType != typeof(Guid) ||
                property.Name.Length <= 2 ||
                !property.Name.EndsWith("Id", StringComparison.Ordinal))
            {
                continue;
            }

            if (EntitiesByName.Value.TryGetValue(property.Name[..^2], out var target))
            {
                yield return (target, property.Name);
            }
        }
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
