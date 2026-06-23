using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Appointments.Commands;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class AppointmentRemindersTests : IntegrationTestBase
{
    public AppointmentRemindersTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<int> RunRemindersAsync(int leadMinutes)
    {
        using var scope = Factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(new SendDueAppointmentRemindersCommand(leadMinutes));
    }

    private async Task<DateTime?> ReminderSentAtAsync(int appointmentId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appt = await db.Appointments.FindAsync(appointmentId);
        return appt!.ReminderSentAt;
    }

    private async Task<int> BookAsync(int minutesFromNow)
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();
        var create = await trainer.PostAsJsonAsync("/api/v1/appointments", new CreateAppointmentRequest
        {
            ClientId = clientId,
            StartsAt = DateTime.UtcNow.AddMinutes(minutesFromNow)
        });
        create.EnsureSuccessStatusCode();
        var dto = await create.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
        return dto!.Id;
    }

    [Fact]
    public async Task A_session_starting_within_the_lead_window_is_reminded_once()
    {
        var id = await BookAsync(minutesFromNow: 30);

        await RunRemindersAsync(leadMinutes: 60);
        var firstStamp = await ReminderSentAtAsync(id);
        firstStamp.Should().NotBeNull();

        // A second pass must not re-send (the stamp stays put).
        await RunRemindersAsync(leadMinutes: 60);
        (await ReminderSentAtAsync(id)).Should().Be(firstStamp);
    }

    [Fact]
    public async Task A_session_outside_the_lead_window_is_not_reminded()
    {
        var id = await BookAsync(minutesFromNow: 300); // 5h away, well beyond a 60-min lead

        await RunRemindersAsync(leadMinutes: 60);

        (await ReminderSentAtAsync(id)).Should().BeNull();
    }
}
