using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class AppointmentsEndpointsTests : IntegrationTestBase
{
    public AppointmentsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    private static DateTime Tomorrow(int hour = 9) => DateTime.UtcNow.Date.AddDays(1).AddHours(hour);

    [Fact]
    public async Task Listing_appointments_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/appointments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_client_cannot_create_an_appointment_returns_403()
    {
        var (client, _, _) = await RegisterUserAsync("Client");

        var response = await client.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = "whoever", StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_trainer_books_a_session_and_it_appears_for_both_parties()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/appointments", new CreateAppointmentRequest
        {
            ClientId = clientId,
            StartsAt = Tomorrow(10),
            DurationMinutes = 45,
            Type = AppointmentType.Online,
            Location = "https://meet.example/abc"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
        created!.Id.Should().BeGreaterThan(0);
        created.Status.Should().Be("Scheduled");
        created.Type.Should().Be("Online");
        created.CounterpartName.Should().Be("Client User");

        var trainerList = await trainer.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        trainerList!.Should().ContainSingle(a => a.Id == created.Id);

        var clientList = await client.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        clientList!.Should().ContainSingle(a => a.Id == created.Id && a.CounterpartName == "Trainer User");
    }

    [Fact]
    public async Task A_trainer_cannot_book_for_someone_elses_client_returns_403()
    {
        var (_, _, trainerA, _) = await CreateLinkedClientAndTrainerAsync();
        var (_, otherClientId, _, _) = await CreateLinkedClientAndTrainerAsync();

        var response = await trainerA.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = otherClientId, StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Booking_for_a_nonexistent_client_returns_404()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var response = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = Guid.NewGuid().ToString(), StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_client_without_a_trainer_cannot_request_a_session_returns_400()
    {
        var (client, _, _) = await RegisterUserAsync("Client");

        var response = await client.PostAsJsonAsync("/api/v1/appointments/request",
            new RequestAppointmentRequest { StartsAt = Tomorrow() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_client_requests_a_session_and_the_trainer_confirms_it()
    {
        var (client, _, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var request = await client.PostAsJsonAsync("/api/v1/appointments/request",
            new RequestAppointmentRequest { StartsAt = Tomorrow(8), DurationMinutes = 60 });
        request.StatusCode.Should().Be(HttpStatusCode.Created);

        var requested = await request.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
        requested!.Status.Should().Be("Requested");

        var confirm = await trainer.PostAsync($"/api/v1/appointments/{requested.Id}/confirm", null);
        confirm.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await trainer.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{requested.Id}", JsonOptions);
        after!.Status.Should().Be("Scheduled");
    }

    [Fact]
    public async Task A_trainer_completes_a_scheduled_session()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = clientId, StartsAt = Tomorrow(11) });
        var created = await create.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        var complete = await trainer.PostAsync($"/api/v1/appointments/{created!.Id}/complete", null);
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await trainer.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{created.Id}", JsonOptions);
        after!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task A_client_cancels_their_own_appointment()
    {
        var (client, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = clientId, StartsAt = Tomorrow(12) });
        var created = await create.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        var cancel = await client.PostAsync($"/api/v1/appointments/{created!.Id}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await client.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{created.Id}", JsonOptions);
        after!.Status.Should().Be("Cancelled");

        // A cancelled session drops out of the calendar list (but is still fetchable by id).
        var list = await client.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        list!.Should().NotContain(a => a.Id == created.Id);
    }

    [Fact]
    public async Task Booking_two_sessions_in_the_same_slot_is_rejected_returns_409()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();
        var slot = Tomorrow(14);

        var first = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = clientId, StartsAt = slot, DurationMinutes = 60 });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Overlapping slot (starts 30 min into the first session) must be refused.
        var clash = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = clientId, StartsAt = slot.AddMinutes(30), DurationMinutes = 60 });
        clash.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var list = await trainer.GetFromJsonAsync<List<AppointmentDto>>("/api/v1/appointments", JsonOptions);
        list!.Count(a => a.StartsAt.Date == slot.Date).Should().Be(1);
    }

    [Fact]
    public async Task Booking_a_session_in_the_past_is_rejected_returns_400()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var response = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = clientId, StartsAt = DateTime.UtcNow.AddDays(-2) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_outsider_cannot_view_someone_elses_appointment_returns_403()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();
        var outsider = await CreateAuthenticatedClientAsync("Trainer");

        var create = await trainer.PostAsJsonAsync("/api/v1/appointments",
            new CreateAppointmentRequest { ClientId = clientId, StartsAt = Tomorrow(13) });
        var created = await create.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        var response = await outsider.GetAsync($"/api/v1/appointments/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
