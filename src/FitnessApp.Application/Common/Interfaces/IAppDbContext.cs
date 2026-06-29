using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence DbContext so CQRS handlers in the Application
/// layer can access data without depending on Infrastructure directly.
/// Add DbSets here as features are migrated to CQRS.
/// </summary>
public interface IAppDbContext
{
    DbSet<Exercise> Exercises { get; }
    DbSet<TrainingPlan> TrainingPlans { get; }
    DbSet<TrainingDay> TrainingDays { get; }
    DbSet<PlannedExercise> PlannedExercises { get; }
    DbSet<PlannedExerciseCompletion> PlannedExerciseCompletions { get; }
    DbSet<PerformedSet> PerformedSets { get; }
    DbSet<Workout> Workouts { get; }
    DbSet<WorkoutExercise> WorkoutExercises { get; }
    DbSet<WorkoutSet> WorkoutSets { get; }
    DbSet<NutritionPlan> NutritionPlans { get; }
    DbSet<NutritionDay> NutritionDays { get; }
    DbSet<Meal> Meals { get; }
    DbSet<MealItem> MealItems { get; }
    DbSet<Device> Devices { get; }
    DbSet<ProgressPhoto> ProgressPhotos { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<TrainerRequest> TrainerRequests { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<TrainingGroup> TrainingGroups { get; }
    DbSet<TrainingGroupMember> TrainingGroupMembers { get; }
    DbSet<GroupAttendance> GroupAttendances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
