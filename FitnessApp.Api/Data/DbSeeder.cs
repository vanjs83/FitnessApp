using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Data;

public static class DbSeeder
{
    private record Template(string Name, string MuscleGroup, ExerciseType Type, string Description);

    private static readonly Template[] DefaultExercises =
    {
        new("Bench Press", "Prsa", ExerciseType.Strength, "Klasični potisak s ravne klupe."),
        new("Squat", "Noge", ExerciseType.Strength, "Čučanj sa šipkom."),
        new("Deadlift", "Leđa", ExerciseType.Strength, "Mrtvo dizanje."),
        new("Overhead Press", "Ramena", ExerciseType.Strength, "Potisak iznad glave."),
        new("Barbell Row", "Leđa", ExerciseType.Strength, "Veslanje šipkom u pretklonu."),
        new("Pull-up", "Leđa", ExerciseType.Strength, "Zgib na vratilu."),
        new("Dips", "Prsa", ExerciseType.Strength, "Sklekovi na razboju."),
        new("Bicep Curl", "Ruke", ExerciseType.Strength, "Pregib s bučicama ili šipkom."),
        new("Tricep Pushdown", "Ruke", ExerciseType.Strength, "Istezanje na sajli."),
        new("Plank", "Core", ExerciseType.Funkcionalni, "Statički položaj na laktovima.")
    };

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

        foreach (var trainer in trainers)
        {
            await SeedDefaultExercisesForTrainerAsync(db, trainer.Id);
        }
    }

    public static async Task SeedDefaultExercisesForTrainerAsync(AppDbContext db, string trainerId)
    {
        var existingNames = await db.Exercises
            .Where(e => e.CreatedByUserId == trainerId)
            .Select(e => e.Name)
            .ToListAsync();

        var existing = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        var toAdd = DefaultExercises
            .Where(t => !existing.Contains(t.Name))
            .Select(t => new Exercise
            {
                Name = t.Name,
                MuscleGroup = t.MuscleGroup,
                Type = t.Type,
                Description = t.Description,
                CreatedByUserId = trainerId
            })
            .ToList();

        if (toAdd.Count == 0) return;

        db.Exercises.AddRange(toAdd);
        await db.SaveChangesAsync();
    }
}
