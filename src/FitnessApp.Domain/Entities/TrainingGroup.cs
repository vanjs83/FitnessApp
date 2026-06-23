namespace FitnessApp.Domain.Entities;

/// <summary>
/// A named group of a trainer's clients, used to book a single training session
/// for several people at once (e.g. a bootcamp or class).
/// </summary>
public class TrainingGroup
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TrainingGroupMember> Members { get; set; } = new List<TrainingGroupMember>();
}
