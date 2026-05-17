namespace FitnessApp.Domain.Entities;

public class TrainingDay
{
    public int Id { get; set; }
    public int TrainingPlanId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public TrainingPlan TrainingPlan { get; set; } = null!;
    public ICollection<PlannedExercise> Exercises { get; set; } = new List<PlannedExercise>();
}
