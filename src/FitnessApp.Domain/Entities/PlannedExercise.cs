namespace FitnessApp.Domain.Entities;

public class PlannedExercise
{
    public int Id { get; set; }
    public int TrainingDayId { get; set; }
    public int ExerciseId { get; set; }
    public int Order { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public decimal TargetWeightKg { get; set; }
    public int? TargetDurationSeconds { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }

    public TrainingDay TrainingDay { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
    public ICollection<PlannedExerciseCompletion> Completions { get; set; } = new List<PlannedExerciseCompletion>();
    public ICollection<PerformedSet> PerformedSets { get; set; } = new List<PerformedSet>();
}
