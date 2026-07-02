using FitnessApp.Api.Extensions;
using FitnessApp.Application;
using FitnessApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Integration tests spin up the app via WebApplicationFactory with a SQLite
// in-memory database; skip the SQL Server migrate/seed step in that environment.
if (!app.Environment.IsEnvironment("Testing"))
    await app.SeedDatabaseAsync();

app.ConfigurePipeline();
app.Run();

// Exposes the implicit Program class so WebApplicationFactory<Program> can boot the host in tests.
public partial class Program;

