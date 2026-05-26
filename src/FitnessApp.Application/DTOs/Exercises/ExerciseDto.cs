using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.Exercises;

public class ExerciseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? MuscleGroup { get; set; }
    public ExerciseType Type { get; set; }
    public bool CanEdit { get; set; }
}

public class CreateExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? MuscleGroup { get; set; }
    public ExerciseType Type { get; set; } = ExerciseType.Strength;
}

public class UpdateExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? MuscleGroup { get; set; }
    public ExerciseType Type { get; set; } = ExerciseType.Strength;
}
