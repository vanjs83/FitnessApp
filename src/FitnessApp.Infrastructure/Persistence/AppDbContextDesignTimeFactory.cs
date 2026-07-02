using FitnessApp.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> builds the model WITHOUT running the app's Program.cs —
/// whose startup calls <c>SeedDatabaseAsync</c> and would migrate/seed the configured (production)
/// database. The connection string below is a placeholder: <c>migrations add</c> generates SQL from
/// the model and never opens a connection, so this never touches any real database.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=DesignTimeOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options, Options.Create(new StorageSettings()));
    }
}
