using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Infrastructure.Identity;

public class IdentityAdminService : IIdentityAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public IdentityAdminService(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    private static AdminUserInfo Map(ApplicationUser u) =>
        new(u.Id, u.Email ?? "", u.FullName, u.CreatedAt, u.TrainerId, u.ProfileImagePath);

    public async Task<IReadOnlyList<AdminUserInfo>> GetUsersInRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        return users.Select(Map).ToList();
    }

    public async Task<AdminUserInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user == null ? null : Map(user);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => await _userManager.FindByEmailAsync(email) != null;

    public async Task<(bool Succeeded, AdminUserInfo? User, IReadOnlyList<string> Errors)> CreateTrainerAsync(
        string email, string? fullName, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, null, result.Errors.Select(e => e.Description).ToList());

        await _userManager.AddToRoleAsync(user, Roles.Trainer);
        return (true, Map(user), Array.Empty<string>());
    }

    public async Task<TrainerDeleteOutcome> DeactivateTrainerAsync(string id, CancellationToken cancellationToken = default)
    {
        var trainer = await _userManager.FindByIdAsync(id);
        if (trainer == null) return TrainerDeleteOutcome.NotFound;
        if (!await _userManager.IsInRoleAsync(trainer, Roles.Trainer))
            return TrainerDeleteOutcome.NotTrainer;

        var clients = await _db.Users.Where(u => u.TrainerId == id).ToListAsync(cancellationToken);
        foreach (var c in clients)
            c.TrainerId = null;

        trainer.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return TrainerDeleteOutcome.Deleted;
    }
}
