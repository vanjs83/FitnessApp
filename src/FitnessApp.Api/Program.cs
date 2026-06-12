using FitnessApp.Api.Extensions;
using System.Drawing.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Integration tests spin up the app via WebApplicationFactory with a SQLite
// in-memory database; skip the SQL Server migrate/seed step in that environment.
if (!app.Environment.IsEnvironment("Testing"))
    await app.SeedDatabaseAsync();

app.ConfigurePipeline();
app.Run();

// Exposes the implicit Program class so WebApplicationFactory<Program> can boot the host in tests.
public partial class Program;

