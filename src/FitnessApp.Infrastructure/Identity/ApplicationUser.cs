using FitnessApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? ProfileImagePath { get; set; }

    public string? TrainerId { get; set; }
    public ApplicationUser? Trainer { get; set; }
    public ICollection<ApplicationUser> Clients { get; set; } = new List<ApplicationUser>();

    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public int? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Goal { get; set; }
    public string? HealthNotes { get; set; }
    public string? ActivityLevel { get; set; }
    public string? Phone { get; set; }

    public int? PreferredWeeklyTrainingCount { get; set; }
    public ExerciseType? PreferredTrainingType { get; set; }
}
