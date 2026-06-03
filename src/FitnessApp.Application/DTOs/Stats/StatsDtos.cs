namespace FitnessApp.Application.DTOs.Stats;

public class ExerciseProgressPointDto
{
    public DateTime Date { get; set; }
    public int SetNumber { get; set; }
    public decimal MaxWeight { get; set; }
    public int TotalReps { get; set; }
    public decimal TotalVolume { get; set; }
    public int SetCount { get; set; }
}

public class ExerciseProgressBySetDto
{
    public int SetNumber { get; set; }
    public List<SetProgressPointDto> Points { get; set; } = new();
}

public class SetProgressPointDto
{
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
    public int Reps { get; set; }
}

public class TrainedExerciseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? MuscleGroup { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class PlanExerciseProgressionDto
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string? MuscleGroup { get; set; }
    public decimal TargetWeightKg { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public List<ExerciseProgressPointDto> Points { get; set; } = new();
}
