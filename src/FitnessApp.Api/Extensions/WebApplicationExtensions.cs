using FitnessApp.Api.Data;
using FitnessApp.Api.Hubs;
using FitnessApp.Application.Storage;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;

namespace FitnessApp.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleSeeder.SeedAsync(roleManager);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SuperAdminSeeder.SeedAsync(userManager, app.Configuration);

        await DbSeeder.SeedAsync(db, userManager);
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseStaticFilesWithStorage();

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat");

        return app;
    }

    private static void UseStaticFilesWithStorage(this WebApplication app)
    {
        var defaultFiles = new DefaultFilesOptions();
        defaultFiles.DefaultFileNames.Clear();
        defaultFiles.DefaultFileNames.Add("landing.html");
        defaultFiles.DefaultFileNames.Add("index.html");
        app.UseDefaultFiles(defaultFiles);
        app.UseStaticFiles();

        var storage = app.Configuration.GetSection("Storage").Get<StorageSettings>() ?? new StorageSettings();
        foreach (var (rawPath, urlPrefix) in new[]
        {
            (storage.ProfileImagesPath, storage.ProfileImagesUrl),
            (storage.ExerciseVideosPath, storage.ExerciseVideosUrl)
        })
        {
            if (string.IsNullOrWhiteSpace(rawPath) || string.IsNullOrWhiteSpace(urlPrefix)) continue;
            var resolved = Path.IsPathRooted(rawPath) ? rawPath : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, rawPath));
            Directory.CreateDirectory(resolved);
            var wwwroot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
            if (!resolved.StartsWith(wwwroot, StringComparison.OrdinalIgnoreCase))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(resolved),
                    RequestPath = urlPrefix.TrimEnd('/')
                });
            }
        }
    }
}
