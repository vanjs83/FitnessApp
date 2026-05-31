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
            .Select(u => new UserInfo(u.Id, u.FullName, u.Email, u.TrainerId))
            .FirstOrDefaultAsync(cancellationToken);
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
