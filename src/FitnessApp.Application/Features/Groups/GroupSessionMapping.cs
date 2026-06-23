using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Groups;

internal static class GroupSessionMapping
{
    /// <summary>Maps a session to a DTO. <paramref name="group"/> supplies the name and member count.</summary>
    public static GroupSessionDto ToDto(GroupSession s, TrainingGroup group) => new()
    {
        Id = s.Id,
        GroupId = s.GroupId,
        GroupName = group.Name,
        MemberCount = group.Members.Count,
        StartsAt = s.StartsAt,
        DurationMinutes = s.DurationMinutes,
        EndsAt = s.EndsAt,
        Status = s.Status.ToString(),
        Type = s.Type.ToString(),
        Location = s.Location,
        Notes = s.Notes
    };
}
