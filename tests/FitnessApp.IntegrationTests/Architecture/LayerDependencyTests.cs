using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace FitnessApp.IntegrationTests.Architecture;

/// <summary>
/// Enforces the Clean Architecture dependency rule across module layers:
/// dependencies point inward only (Domain → nothing, Application → Domain,
/// Infrastructure/Api → inner layers). Mirrors the reference template's LayerTests.
/// </summary>
public class LayerDependencyTests : ArchitectureTestBase
{
    [Fact]
    public void Domain_Should_NotDependOn_Application()
        => AssertNoDependency(DomainAssembly, ApplicationAssembly);

    [Fact]
    public void Domain_Should_NotDependOn_Infrastructure()
        => AssertNoDependency(DomainAssembly, InfrastructureAssembly);

    [Fact]
    public void Domain_Should_NotDependOn_Api()
        => AssertNoDependency(DomainAssembly, ApiAssembly);

    [Fact]
    public void Application_Should_NotDependOn_Infrastructure()
        => AssertNoDependency(ApplicationAssembly, InfrastructureAssembly);

    [Fact]
    public void Application_Should_NotDependOn_Api()
        => AssertNoDependency(ApplicationAssembly, ApiAssembly);

    [Fact]
    public void Infrastructure_Should_NotDependOn_Api()
        => AssertNoDependency(InfrastructureAssembly, ApiAssembly);

    private static void AssertNoDependency(Assembly source, Assembly forbidden)
    {
        var forbiddenName = forbidden.GetName().Name!;

        TestResult result = Types.InAssembly(source)
            .Should()
            .NotHaveDependencyOn(forbiddenName)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "{0} must not depend on {1}, but these types do: {2}",
            source.GetName().Name,
            forbiddenName,
            result.FailingTypeNames is null ? "" : string.Join(", ", result.FailingTypeNames));
    }
}
