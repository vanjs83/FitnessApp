namespace FitnessApp.Domain.Entities;

public class Workout
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
}

public class WorkoutExercise
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int ExerciseId { get; set; }
    public int Order { get; set; }

    public Workout Workout { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();
}

public class WorkoutSet
{
    public int Id { get; set; }
    public int WorkoutExerciseId { get; set; }
    public int SetNumber { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }

    public WorkoutExercise WorkoutExercise { get; set; } = null!;
}
