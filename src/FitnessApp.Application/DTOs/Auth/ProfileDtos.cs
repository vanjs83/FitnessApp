using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.Auth;

public class MeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? TrainerId { get; set; }
    public string? TrainerName { get; set; }
    public string? TrainerImageUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class PersonalProfileDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
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
    public string? ProfileImageUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class UpdatePersonalProfileRequest
{
    public string? FullName { get; set; }
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
