namespace FitnessApp.Domain.Entities;

/// <summary>
/// A client's request to be coached by a trainer. The trainer must accept it
/// before the connection (ApplicationUser.TrainerId) is established.
/// </summary>
public class TrainerRequest
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string TrainerId { get; set; } = string.Empty;
    public TrainerRequestStatus Status { get; set; } = TrainerRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}

public enum TrainerRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3
}
