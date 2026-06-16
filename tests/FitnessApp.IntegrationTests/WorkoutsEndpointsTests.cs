using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Workouts;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class WorkoutsEndpointsTests : IntegrationTestBase
{
    public WorkoutsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Listing_workouts_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/workouts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_trainer_cannot_access_client_workouts_returns_403()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var response = await trainer.GetAsync("/api/v1/workouts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_client_without_a_trainer_cannot_log_a_workout_returns_400()
    {
        var (client, _, _) = await RegisterUserAsync("Client");

        var response = await client.PostAsJsonAsync("/api/v1/workouts",
            new CreateWorkoutRequest { Name = "Leg day" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_linked_client_logs_a_workout_and_it_appears_in_the_list()
    {
        var (client, _, _, _) = await CreateLinkedClientAndTrainerAsync();

        var create = await client.PostAsJsonAsync("/api/v1/workouts",
            new CreateWorkoutRequest { Name = "Leg day", DurationMinutes = 60 });
        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await create.Content.ReadFromJsonAsync<WorkoutDetailDto>(JsonOptions);
        created!.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Leg day");

        var list = await client.GetFromJsonAsync<PagedResult<WorkoutListItemDto>>("/api/v1/workouts", JsonOptions);
        list!.Items.Should().ContainSingle(w => w.Id == created.Id && w.Name == "Leg day");
    }
}
