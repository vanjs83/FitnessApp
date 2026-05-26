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
    public string Name { get; set; } = string.Empty;
    public DateTime? PerformedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
}

public class AddWorkoutExerciseRequest
{
    public int ExerciseId { get; set; }
    public int Order { get; set; }
}

public class AddWorkoutSetRequest
{
    public int WorkoutExerciseId { get; set; }
    public int SetNumber { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
}
