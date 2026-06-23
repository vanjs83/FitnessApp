using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Groups;

internal static class GroupMapping
{
    /// <summary>Maps a group (with its <see cref="TrainingGroup.Members"/> loaded) to a DTO, resolving member display names.</summary>
    public static async Task<TrainingGroupDto> ToDtoAsync(
        TrainingGroup group, IUserDirectory users, CancellationToken cancellationToken)
    {
        var memberIds = group.Members.Select(m => m.ClientId).ToList();
        var names = await users.GetDisplayNamesAsync(memberIds, cancellationToken);

        return new TrainingGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            MemberCount = memberIds.Count,
            Members = group.Members
                .Select(m => new GroupMemberDto
                {
                    ClientId = m.ClientId,
                    FullName = names.TryGetValue(m.ClientId, out var n) ? n : ""
                })
                .OrderBy(m => m.FullName)
                .ToList()
        };
    }
}
