using FitnessApp.Api.Data;
using FitnessApp.Application.Interfaces;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FitnessApp.IntegrationTests;

/// <summary>
/// Boots the real API in-process (full HTTP pipeline: routing, JWT auth, MediatR,
/// FluentValidation, EF Core) but swaps SQL Server for a SQLite in-memory database,
/// so the tests need no external dependency and run anywhere, including CI.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // A single open connection keeps the in-memory database alive for the factory's
    // lifetime; once the last connection closes, SQLite discards the schema and data.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs skips the SQL Server migrate/seed when the environment is "Testing".
        builder.UseEnvironment("Testing");

        // Keep the reminder background worker idle during tests so it never polls/sends.
        builder.UseSetting("Reminders:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            // Drop the SQL Server AppDbContext that Infrastructure registered. In EF Core 9
            // the provider is also wired through IDbContextOptionsConfiguration<AppDbContext>,
            // so that must go too — otherwise SQL Server and SQLite both end up on the options
            // and EF refuses to start ("multiple relational provider configurations"). Match it
            // by name to stay resilient across EF versions.
            var efDescriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(AppDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration") &&
                 d.ServiceType.GenericTypeArguments is [var ctx] && ctx == typeof(AppDbContext)))
                .ToList();
            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            // ...and replace it with the shared SQLite connection. The existing
            // IAppDbContext registration (sp => sp.GetRequiredService<AppDbContext>()) still applies.
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // Swap the real SMTP email service for a no-op so tests never hit a mail server.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, FakeEmailService>();
        });
    }

    /// <summary>
    /// Creates the schema and seeds the Identity roles. Safe to call repeatedly
    /// (EnsureCreated and the role seeder are both idempotent).
    /// </summary>
    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        RoleSeeder.SeedAsync(roleManager).GetAwaiter().GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
