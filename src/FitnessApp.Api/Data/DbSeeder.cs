using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        var trainers = await userManager.GetUsersInRoleAsync(Roles.Trainer);
        if (!trainers.Any()) return;

        var firstTrainer = trainers.OrderBy(u => u.CreatedAt).First();

        var orphans = await db.Exercises
            .Where(e => e.CreatedByUserId == null)
            .ToListAsync();
        if (orphans.Count > 0)
        {
            foreach (var o in orphans) o.CreatedByUserId = firstTrainer.Id;
            await db.SaveChangesAsync();
        }
    }
}
