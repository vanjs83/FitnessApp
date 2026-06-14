using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Trainers;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class TrainersEndpointsTests : IntegrationTestBase
{
    public TrainersEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task The_public_trainer_list_contains_a_registered_trainer()
    {
        var (_, trainerId, _) = await RegisterUserAsync("Trainer");

        // Endpoint is [AllowAnonymous] â€” no token needed.
        var anonymous = Factory.CreateClient();
        var trainers = await anonymous.GetFromJsonAsync<List<TrainerListItemDto>>("/api/v1/trainers", JsonOptions);

        trainers.Should().Contain(t => t.Id == trainerId);
    }

    [Fact]
    public async Task A_client_cannot_list_a_trainers_clients_returns_403()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.GetAsync("/api/v1/trainers/me/clients");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_trainer_sees_a_linked_client_in_their_client_list()
    {
        var (_, clientId, trainer, _) = await CreateLinkedClientAndTrainerAsync();

        var clients = await trainer.GetFromJsonAsync<List<ClientListItemDto>>("/api/v1/trainers/me/clients", JsonOptions);

        clients.Should().ContainSingle(c => c.Id == clientId);
    }
}
