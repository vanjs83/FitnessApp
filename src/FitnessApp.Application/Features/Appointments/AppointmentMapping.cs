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
            dto.ConfirmedCount = a.Attendances?.Count ?? 0;
            // Attendance is a member's own state; the trainer doesn't attend, so it's null for them.
            dto.IsAttending = a.TrainerId == currentUserId
                ? null
                : a.Attendances?.Any(at => at.ClientId == currentUserId) ?? false;
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
