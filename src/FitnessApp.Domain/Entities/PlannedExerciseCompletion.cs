namespace FitnessApp.Domain.Entities;

public class PlannedExerciseCompletion
{
    public int Id { get; set; }
    public int PlannedExerciseId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public PlannedExercise PlannedExercise { get; set; } = null!;
}
