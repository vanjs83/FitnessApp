using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.DTOs.Trainers;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class TrainerRequestsEndpointsTests : IntegrationTestBase
{
    public TrainerRequestsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Listing_my_requests_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/trainer-requests/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_trainer_cannot_send_a_request_returns_403()
    {
        var (trainer, _, _) = await RegisterUserAsync("Trainer");

        var response = await trainer.PostAsJsonAsync("/api/v1/trainer-requests",
            new CreateTrainerRequestRequest { TrainerId = "anyone" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_client_cannot_view_incoming_requests_returns_403()
    {
        var (client, _, _) = await RegisterUserAsync("Client");

        var response = await client.GetAsync("/api/v1/trainer-requests/incoming");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Client_sends_a_request_trainer_accepts_and_the_link_is_established()
    {
        var (client, _, _) = await RegisterUserAsync("Client");
        var (trainer, trainerId, _) = await RegisterUserAsync("Trainer");

        // Client sends the request.
        var send = await client.PostAsJsonAsync("/api/v1/trainer-requests",
            new CreateTrainerRequestRequest { TrainerId = trainerId });
        send.StatusCode.Should().Be(HttpStatusCode.Created);
        var mine = await send.Content.ReadFromJsonAsync<MyTrainerRequestDto>(JsonOptions);
        mine!.Status.Should().Be("Pending");
        mine.TrainerId.Should().Be(trainerId);

        // Trainer sees it in the incoming list.
        var incoming = await trainer.GetFromJsonAsync<List<IncomingTrainerRequestDto>>(
            "/api/v1/trainer-requests/incoming", JsonOptions);
        incoming.Should().ContainSingle();
        var requestId = incoming!.Single().Id;

        // Trainer accepts.
        var accept = await trainer.PostAsync($"/api/v1/trainer-requests/{requestId}/accept", null);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);

        // The client is now linked to the trainer.
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/auth/me", JsonOptions);
        me!.TrainerId.Should().Be(trainerId);
    }

    [Fact]
    public async Task A_second_pending_request_from_the_same_client_returns_400()
    {
        var (client, _, _) = await RegisterUserAsync("Client");
        var (_, trainerId, _) = await RegisterUserAsync("Trainer");
        var payload = new CreateTrainerRequestRequest { TrainerId = trainerId };

        var first = await client.PostAsJsonAsync("/api/v1/trainer-requests", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/v1/trainer-requests", payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
