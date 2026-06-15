using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.Progress;

public class ProgressPhotoDto
{
    public int Id { get; set; }
    public int? PlanId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public ProgressPose Pose { get; set; }
    public DateTime TakenOn { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
