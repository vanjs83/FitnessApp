using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingDay> TrainingDays => Set<TrainingDay>();
    public DbSet<PlannedExercise> PlannedExercises => Set<PlannedExercise>();
    public DbSet<PlannedExerciseCompletion> PlannedExerciseCompletions => Set<PlannedExerciseCompletion>();
    public DbSet<PerformedSet> PerformedSets => Set<PerformedSet>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<NutritionDay> NutritionDays => Set<NutritionDay>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(u =>
        {
            u.HasOne(x => x.Trainer)
                .WithMany(x => x.Clients)
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.NoAction);
            u.HasIndex(x => x.TrainerId);
            u.HasQueryFilter(x => x.IsActive);
        });

        builder.Entity<Exercise>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.MuscleGroup).HasMaxLength(60);
            e.HasIndex(x => x.Name);
        });

        builder.Entity<TrainingPlan>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.TrainerId).IsRequired();
            e.Property(x => x.TrainerExpectations).HasMaxLength(2000);
            e.HasIndex(x => new { x.TrainerId, x.ClientId });
            e.HasIndex(x => x.ClientId);
            e.HasIndex(x => new { x.TrainerId, x.IsTemplate });

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TrainingDay>(e =>
        {
            e.Property(x => x.Label).IsRequired().HasMaxLength(60);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.TrainingPlan)
                .WithMany(p => p.Days)
                .HasForeignKey(x => x.TrainingPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TrainingPlanId, x.DayOfWeek });
        });

        builder.Entity<PlannedExercise>(e =>
        {
            e.Property(x => x.TargetWeightKg).HasColumnType("decimal(7,2)");
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.TrainingDay)
                .WithMany(d => d.Exercises)
                .HasForeignKey(x => x.TrainingDayId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PlannedExerciseCompletion>(e =>
        {
            e.Property(x => x.ClientId).IsRequired();
            e.HasIndex(x => new { x.PlannedExerciseId, x.CompletedAt });
            e.HasIndex(x => x.ClientId);

            e.HasOne(x => x.PlannedExercise)
                .WithMany(pe => pe.Completions)
                .HasForeignKey(x => x.PlannedExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PerformedSet>(e =>
        {
            e.Property(x => x.ActualWeightKg).HasColumnType("decimal(7,2)");
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.PlannedExerciseId, x.PerformedAt });

            e.HasOne(x => x.PlannedExercise)
                .WithMany(pe => pe.PerformedSets)
                .HasForeignKey(x => x.PlannedExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Workout>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.ClientId).IsRequired();
            e.Property(x => x.TrainerId).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ClientId, x.PerformedAt });

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkoutExercise>(e =>
        {
            e.HasOne(x => x.Workout)
                .WithMany(w => w.Exercises)
                .HasForeignKey(x => x.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkoutSet>(e =>
        {
            e.Property(x => x.Weight).HasColumnType("decimal(7,2)");
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.WorkoutExercise)
                .WithMany(we => we.Sets)
                .HasForeignKey(x => x.WorkoutExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NutritionPlan>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.TrainerId).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.TrainerId, x.ClientId });
            e.HasIndex(x => x.ClientId);
            e.HasIndex(x => new { x.TrainerId, x.IsTemplate });

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NutritionDay>(e =>
        {
            e.Property(x => x.Label).IsRequired().HasMaxLength(60);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.NutritionPlan)
                .WithMany(p => p.Days)
                .HasForeignKey(x => x.NutritionPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NutritionPlanId, x.DayOfWeek });
        });

        builder.Entity<Meal>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.NutritionDay)
                .WithMany(d => d.Meals)
                .HasForeignKey(x => x.NutritionDayId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NutritionDayId, x.Order });
        });

        builder.Entity<MealItem>(e =>
        {
            e.Property(x => x.Description).IsRequired().HasMaxLength(300);
            e.Property(x => x.Quantity).HasMaxLength(60);
            e.Property(x => x.ProteinG).HasColumnType("decimal(7,2)");
            e.Property(x => x.CarbsG).HasColumnType("decimal(7,2)");
            e.Property(x => x.FatG).HasColumnType("decimal(7,2)");
            e.HasOne(x => x.Meal)
                .WithMany(m => m.Items)
                .HasForeignKey(x => x.MealId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
