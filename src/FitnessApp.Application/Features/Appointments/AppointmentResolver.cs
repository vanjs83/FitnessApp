using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Appointments;

/// <summary>
/// Maps a single appointment to its DTO, resolving the counterpart's display name.
/// Shared by the queries and the commands that return the affected appointment.
/// </summary>
internal static class AppointmentResolver
{
    public static async Task<AppointmentDto> ToDtoAsync(
        Appointment appointment, string currentUserId, IUserDirectory users, CancellationToken cancellationToken)
    {
        var counterpartId = appointment.TrainerId == currentUserId ? appointment.ClientId : appointment.TrainerId;
        var names = await users.GetDisplayNamesAsync(new[] { counterpartId }, cancellationToken);
        return AppointmentMapping.ToDto(appointment, currentUserId, names);
    }
}
