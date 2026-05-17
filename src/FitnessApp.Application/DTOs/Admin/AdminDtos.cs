using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Application.DTOs.Admin;

public class TrainerAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ClientCount { get; set; }
}

public class CreateTrainerRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }
}

public class ClientAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TrainerName { get; set; }
    public int PerformedSetCount { get; set; }
}

public class PlanAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DayCount { get; set; }
    public int PerformedSetCount { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? TrainerName { get; set; }
}

public class AdminStatsDto
{
    public int TrainersCount { get; set; }
    public int ClientsCount { get; set; }
    public int ClientsWithoutTrainer { get; set; }
    public int PlansCount { get; set; }
    public int PerformedSetsCount { get; set; }
    public int ExercisesCount { get; set; }
}
