namespace FitnessApp.Domain.Entities;

/// <summary>One client's membership in a <see cref="TrainingGroup"/>.</summary>
public class TrainingGroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TrainingGroup? Group { get; set; }
}
