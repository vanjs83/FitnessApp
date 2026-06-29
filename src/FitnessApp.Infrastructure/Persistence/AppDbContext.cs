using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Storage;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    private readonly StorageSettings _storage;

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<StorageSettings> storage) : base(options)
    {
        _storage = storage.Value;
    }

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
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<TrainerRequest> TrainerRequests => Set<TrainerRequest>();
    public DbSet<ProgressPhoto> ProgressPhotos => Set<ProgressPhoto>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<TrainingGroup> TrainingGroups => Set<TrainingGroup>();
    public DbSet<TrainingGroupMember> TrainingGroupMembers => Set<TrainingGroupMember>();
    public DbSet<GroupAttendance> GroupAttendances => Set<GroupAttendance>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configurations that depend on StorageSettings can't be discovered by the
        // assembly scan (no parameterless ctor), so apply them explicitly.
        builder.ApplyConfiguration(new ApplicationUserConfiguration(_storage));
        builder.ApplyConfiguration(new ExerciseConfiguration(_storage));
        builder.ApplyConfiguration(new ProgressPhotoConfiguration(_storage));

        // Everything else: one IEntityTypeConfiguration<T> per entity in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
