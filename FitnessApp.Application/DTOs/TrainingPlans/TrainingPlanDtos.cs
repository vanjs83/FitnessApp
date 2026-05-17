using System.ComponentModel.DataAnnotations;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.TrainingPlans;

public class TrainingPlanListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DayCount { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus PaymentStatus { get; set; }
}

public class TrainingPlanDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TrainerExpectations { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaymentClaimedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsLocked { get; set; }
    public List<TrainingDayDto> Days { get; set; } = new();
}

public class TrainingPlanTemplateListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TrainerExpectations { get; set; }
    public int DayCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTrainingPlanTemplateRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? TrainerExpectations { get; set; }
}

public class UpdateTrainingPlanTemplateRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? TrainerExpectations { get; set; }
}

public class CloneTemplateToClientRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(2000)]
    public string? TrainerExpectations { get; set; }

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "EUR";
}

public class TrainingDayDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsCompletedToday { get; set; }
    public List<PlannedExerciseDto> Exercises { get; set; } = new();
}

public class PlannedExerciseDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Order { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public decimal TargetWeightKg { get; set; }
    public int? TargetDurationSeconds { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }
    public int CompletionsCount { get; set; }
    public bool IsCompletedToday { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public List<PerformedSetDto> RecentPerformedSets { get; set; } = new();
}

public class PerformedSetDto
{
    public int Id { get; set; }
    public int PlannedExerciseId { get; set; }
    public int SetNumber { get; set; }
    public int ActualReps { get; set; }
    public decimal ActualWeightKg { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
}

public class LogPerformedSetRequest
{
    [Range(1, 50)]
    public int SetNumber { get; set; }

    [Range(1, 1000)]
    public int ActualReps { get; set; }

    [Range(0, 9999)]
    public decimal ActualWeightKg { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class CreateTrainingPlanRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(2000)]
    public string? TrainerExpectations { get; set; }

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "EUR";
}

public class UpdateTrainingPlanRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(2000)]
    public string? TrainerExpectations { get; set; }

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "EUR";
}

public class AddTrainingDayRequest
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required, MaxLength(60)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class AddPlannedExerciseRequest
{
    [Required]
    public int ExerciseId { get; set; }
    public int Order { get; set; }

    [Range(1, 50)]
    public int TargetSets { get; set; }

    [Range(0, 1000)]
    public int TargetReps { get; set; }

    [Range(0, 9999)]
    public decimal TargetWeightKg { get; set; }

    [Range(1, 7200)]
    public int? TargetDurationSeconds { get; set; }

    [Range(0, 3600)]
    public int? RestSeconds { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
