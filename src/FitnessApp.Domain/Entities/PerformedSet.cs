namespace FitnessApp.Domain.Entities;

public class PerformedSet
{
    public int Id { get; set; }
    public int PlannedExerciseId { get; set; }
    public int SetNumber { get; set; }
    public int ActualReps { get; set; }
    public decimal ActualWeightKg { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public PlannedExercise PlannedExercise { get; set; } = null!;
}
