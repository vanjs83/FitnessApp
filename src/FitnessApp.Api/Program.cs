using FitnessApp.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

await app.SeedDatabaseAsync();
app.ConfigurePipeline();
app.Run();
