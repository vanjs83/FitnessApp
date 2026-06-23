using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Appointments;

internal static class AppointmentMapping
{
    public static AppointmentDto ToDto(Appointment a, string currentUserId, IReadOnlyDictionary<string, string> names)
    {
        var dto = new AppointmentDto
        {
            Id = a.Id,
            TrainerId = a.TrainerId,
            ClientId = a.ClientId,
            StartsAt = a.StartsAt,
            DurationMinutes = a.DurationMinutes,
            EndsAt = a.EndsAt,
            Status = a.Status.ToString(),
            Type = a.Type.ToString(),
            Location = a.Location,
            Notes = a.Notes
        };

        if (a.IsGroup)
        {
            // For a group session the counterpart is the group itself (Group must be loaded).
            dto.IsGroup = true;
            dto.GroupId = a.GroupId;
            dto.GroupName = a.Group?.Name ?? "";
            dto.MemberCount = a.Group?.Members.Count ?? 0;
            dto.CounterpartId = a.GroupId?.ToString() ?? "";
            dto.CounterpartName = dto.GroupName;
        }
        else
        {
            var counterpartId = a.TrainerId == currentUserId ? a.ClientId : a.TrainerId;
            dto.CounterpartId = counterpartId ?? "";
            dto.CounterpartName = counterpartId is not null && names.TryGetValue(counterpartId, out var n) ? n : "";
        }
        return dto;
    }
}
