using System.Reflection;
using FitnessApp.Application;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;

namespace FitnessApp.IntegrationTests.Architecture;

/// <summary>
/// Anchors one type per layer so the dependency rules target each assembly by name,
/// keeping the tests resilient to file moves within a layer. Modeled on the
/// reference Clean Architecture template's BaseTest.
/// </summary>
public abstract class ArchitectureTestBase
{
    protected static readonly Assembly DomainAssembly = typeof(Exercise).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(AppDbContext).Assembly;
    protected static readonly Assembly ApiAssembly = typeof(Program).Assembly;
}
