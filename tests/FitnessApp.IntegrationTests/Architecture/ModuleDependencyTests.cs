using FluentAssertions;
using NetArchTest.Rules;

namespace FitnessApp.IntegrationTests.Architecture;

/// <summary>
/// Guards vertical-slice isolation: feature modules under
/// FitnessApp.Application.Features.* must not reference one another except for the
/// dependencies explicitly documented below. New, unlisted cross-module coupling
/// fails the test so it surfaces in review instead of silently growing.
/// </summary>
public class ModuleDependencyTests : ArchitectureTestBase
{
    private const string Prefix = "FitnessApp.Application.Features.";

    // Documented, intentional cross-module dependencies (acyclic). Keep this in sync
    // when a slice legitimately needs another; anything else is a violation.
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["Trainers"] = new[] { "Workouts", "Progress" },
        ["Groups"] = new[] { "Trainers", "Messaging" },
        ["Appointments"] = new[] { "Trainers", "Groups" },
        ["Admin"] = new[] { "Messaging" },
    };

    [Fact]
    public void Feature_Modules_Should_Only_Depend_On_Allowed_Modules()
    {
        var modules = DiscoverModules();
        var edges = new List<string>();

        foreach (var source in modules)
        {
            foreach (var target in modules)
            {
                if (source == target) continue;

                TestResult result = Types.InNamespace(Prefix + source)
                    .Should()
                    .NotHaveDependencyOn(Prefix + target)
                    .GetResult();

                if (!result.IsSuccessful)
                    edges.Add($"{source} -> {target}");
            }
        }

        var violations = edges.Where(e => !IsAllowed(e)).ToList();

        violations.Should().BeEmpty(
            "feature modules must only depend on documented modules. Unlisted coupling: [{0}]. Full module graph: [{1}]",
            string.Join("; ", violations),
            string.Join("; ", edges));
    }

    private static bool IsAllowed(string edge)
    {
        var parts = edge.Split(" -> ");
        return Allowed.TryGetValue(parts[0], out var targets) && targets.Contains(parts[1]);
    }

    private static List<string> DiscoverModules() =>
        ApplicationAssembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns is not null && ns.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(ns => ns!.Substring(Prefix.Length).Split('.')[0])
            .Distinct()
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();
}
