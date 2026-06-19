using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Appointments;

internal static class AppointmentMapping
{
    public static AppointmentDto ToDto(Appointment a, string currentUserId, IReadOnlyDictionary<string, string> names)
    {
        var counterpartId = a.TrainerId == currentUserId ? a.ClientId : a.TrainerId;
        return new AppointmentDto
        {
            Id = a.Id,
            TrainerId = a.TrainerId,
            ClientId = a.ClientId,
            CounterpartId = counterpartId,
            CounterpartName = names.TryGetValue(counterpartId, out var n) ? n : "",
            StartsAt = a.StartsAt,
            DurationMinutes = a.DurationMinutes,
            EndsAt = a.EndsAt,
            Status = a.Status.ToString(),
            Type = a.Type.ToString(),
            Location = a.Location,
            Notes = a.Notes
        };
    }
}
