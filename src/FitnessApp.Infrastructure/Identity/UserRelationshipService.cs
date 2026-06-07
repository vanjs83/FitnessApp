using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace FitnessApp.Infrastructure.Identity;

public class UserRelationshipService : IUserRelationshipService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRelationshipService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<AssignTrainerOutcome> AssignTrainerAsync(string clientId, string trainerId, CancellationToken cancellationToken = default)
    {
        var client = await _userManager.FindByIdAsync(clientId);
        if (client == null) return AssignTrainerOutcome.ClientNotFound;
        if (!string.IsNullOrEmpty(client.TrainerId)) return AssignTrainerOutcome.ClientAlreadyHasTrainer;

        client.TrainerId = trainerId;
        await _userManager.UpdateAsync(client);
        return AssignTrainerOutcome.Assigned;
    }

    public async Task<(bool Succeeded, AdminUserInfo? User, IReadOnlyList<string> Errors)> CreateClientAsync(
        string email, string? fullName, string password, string trainerId, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            TrainerId = trainerId
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, null, result.Errors.Select(e => e.Description).ToList());

        await _userManager.AddToRoleAsync(user, Roles.Client);
        return (true, new AdminUserInfo(user.Id, user.Email!, user.FullName, user.CreatedAt, user.TrainerId, user.ProfileImagePath), Array.Empty<string>());
    }
}
