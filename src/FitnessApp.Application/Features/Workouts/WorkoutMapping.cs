using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Workouts;

internal static class WorkoutMapping
{
    public static WorkoutDetailDto MapDetail(Workout w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        PerformedAt = w.PerformedAt,
        DurationMinutes = w.DurationMinutes,
        Notes = w.Notes,
        Exercises = w.Exercises
            .OrderBy(we => we.Order)
            .Select(we => new WorkoutExerciseDto
            {
                Id = we.Id,
                ExerciseId = we.ExerciseId,
                ExerciseName = we.Exercise?.Name ?? "",
                Order = we.Order,
                Sets = we.Sets
                    .OrderBy(s => s.SetNumber)
                    .Select(s => new WorkoutSetDto
                    {
                        Id = s.Id,
                        SetNumber = s.SetNumber,
                        Weight = s.Weight,
                        Reps = s.Reps,
                        Notes = s.Notes
                    }).ToList()
            }).ToList()
    };
}
