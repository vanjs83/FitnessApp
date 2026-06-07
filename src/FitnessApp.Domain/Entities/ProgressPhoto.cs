namespace FitnessApp.Domain.Entities;

public class ProgressPhoto
{
    public int Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string ImagePath { get; set; } = null!;
    public ProgressPose Pose { get; set; }
    public DateTime TakenOn { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
