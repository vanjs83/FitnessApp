namespace FitnessApp.Application.DTOs.Trainers;

public class CreateTrainerRequestRequest
{
    public string TrainerId { get; set; } = string.Empty;
    public string? Language { get; set; }
}

/// <summary>A request as seen by the client who sent it.</summary>
public class MyTrainerRequestDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string? TrainerImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>A pending request as seen by the trainer who received it.</summary>
public class IncomingTrainerRequestDto
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
