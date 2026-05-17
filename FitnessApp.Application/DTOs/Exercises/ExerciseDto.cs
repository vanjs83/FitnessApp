using System.ComponentModel.DataAnnotations;
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
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    [MaxLength(60)]
    public string? MuscleGroup { get; set; }

    public ExerciseType Type { get; set; } = ExerciseType.Strength;
}

public class UpdateExerciseRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    [MaxLength(60)]
    public string? MuscleGroup { get; set; }

    public ExerciseType Type { get; set; } = ExerciseType.Strength;
}
