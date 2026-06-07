using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Infrastructure.Identity;

public class UserDirectory : IUserDirectory
{
    private readonly AppDbContext _db;

    public UserDirectory(AppDbContext db) => _db = db;

    public async Task<UserInfo?> FindAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserInfo(u.Id, u.FullName, u.Email, u.TrainerId, u.ProfileImagePath, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserInfo>> GetLinkedPartnersAsync(string userId, CancellationToken cancellationToken = default)
    {
        var me = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.TrainerId })
            .FirstOrDefaultAsync(cancellationToken);
        if (me == null) return new List<UserInfo>();

        return await _db.Users
            .Where(u => u.TrainerId == me.Id || (me.TrainerId != null && u.Id == me.TrainerId))
            .Select(u => new UserInfo(u.Id, u.FullName, u.Email, u.TrainerId, u.ProfileImagePath, u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserInfo>> GetClientsOfAsync(string trainerId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Where(u => u.TrainerId == trainerId)
            .OrderBy(u => u.FullName ?? u.Email)
            .Select(u => new UserInfo(u.Id, u.FullName, u.Email, u.TrainerId, u.ProfileImagePath, u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AreLinkedAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(otherUserId) || otherUserId == userId) return false;
        var pair = await _db.Users
            .Where(u => u.Id == userId || u.Id == otherUserId)
            .Select(u => new { u.Id, u.TrainerId })
            .ToListAsync(cancellationToken);
        var me = pair.FirstOrDefault(u => u.Id == userId);
        var partner = pair.FirstOrDefault(u => u.Id == otherUserId);
        if (me == null || partner == null) return false;
        return partner.TrainerId == me.Id || me.TrainerId == partner.Id;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string>();

        return await _db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email ?? "", cancellationToken);
    }
}
