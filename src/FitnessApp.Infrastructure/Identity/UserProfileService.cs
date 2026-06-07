using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Infrastructure.Identity;

public class UserProfileService : IUserProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserProfileService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<PersonalProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var u = await _userManager.FindByIdAsync(userId);
        if (u == null) return null;

        var roles = await _userManager.GetRolesAsync(u);

        return new PersonalProfileDto
        {
            FullName = u.FullName,
            Email = u.Email,
            BirthDate = u.BirthDate,
            Gender = u.Gender,
            HeightCm = u.HeightCm,
            WeightKg = u.WeightKg,
            Goal = u.Goal,
            HealthNotes = u.HealthNotes,
            ActivityLevel = u.ActivityLevel,
            Phone = u.Phone,
            PreferredWeeklyTrainingCount = u.PreferredWeeklyTrainingCount,
            PreferredTrainingType = u.PreferredTrainingType,
            ProfileImageUrl = u.ProfileImagePath,
            Role = roles.FirstOrDefault() ?? ""
        };
    }
}
