using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Application.DTOs.Workouts;

public class WorkoutListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public int ExerciseCount { get; set; }
}

public class WorkoutDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public List<WorkoutExerciseDto> Exercises { get; set; } = new();
}

public class WorkoutExerciseDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<WorkoutSetDto> Sets { get; set; } = new();
}

public class WorkoutSetDto
{
    public int Id { get; set; }
    public int SetNumber { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

public class CreateWorkoutRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateTime? PerformedAt { get; set; }

    [Range(1, 1000)]
    public int? DurationMinutes { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class AddWorkoutExerciseRequest
{
    [Required]
    public int ExerciseId { get; set; }
    public int Order { get; set; }
}

public class AddWorkoutSetRequest
{
    [Required]
    public int WorkoutExerciseId { get; set; }

    [Range(1, 50)]
    public int SetNumber { get; set; }

    [Range(0, 9999)]
    public decimal Weight { get; set; }

    [Range(0, 1000)]
    public int Reps { get; set; }
}
