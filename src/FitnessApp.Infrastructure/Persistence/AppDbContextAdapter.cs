using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Infrastructure.Persistence;

/// <summary>
/// Exposes <see cref="AppDbContext"/> to the Application layer through <see cref="IAppDbContext"/>
/// without the DbContext itself taking a dependency on the Application assembly.
/// </summary>
public class AppDbContextAdapter : IAppDbContext
{
    private readonly AppDbContext _context;

    public AppDbContextAdapter(AppDbContext context) => _context = context;

    public DbSet<Exercise> Exercises => _context.Exercises;
    public DbSet<PlannedExercise> PlannedExercises => _context.PlannedExercises;
    public DbSet<PerformedSet> PerformedSets => _context.PerformedSets;
    public DbSet<Workout> Workouts => _context.Workouts;
    public DbSet<WorkoutExercise> WorkoutExercises => _context.WorkoutExercises;
    public DbSet<WorkoutSet> WorkoutSets => _context.WorkoutSets;
    public DbSet<NutritionPlan> NutritionPlans => _context.NutritionPlans;
    public DbSet<NutritionDay> NutritionDays => _context.NutritionDays;
    public DbSet<Meal> Meals => _context.Meals;
    public DbSet<MealItem> MealItems => _context.MealItems;
    public DbSet<Device> Devices => _context.Devices;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
