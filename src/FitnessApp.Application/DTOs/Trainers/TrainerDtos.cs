namespace FitnessApp.Application.DTOs.Trainers;

public class TrainerListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class ClientListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlanCount { get; set; }
    public int PerformedSetCount { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class CreateClientRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Language { get; set; }
}
