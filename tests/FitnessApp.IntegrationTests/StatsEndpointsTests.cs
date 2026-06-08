using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Stats;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class StatsEndpointsTests : IntegrationTestBase
{
    public StatsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Stats_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/stats/my-exercises");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_new_user_has_no_trained_exercises()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var trained = await client.GetFromJsonAsync<List<TrainedExerciseDto>>("/api/stats/my-exercises", JsonOptions);

        trained.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Exercise_progress_for_an_untrained_exercise_is_empty()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var points = await client.GetFromJsonAsync<List<ExerciseProgressPointDto>>(
            "/api/stats/exercise-progress/123", JsonOptions);

        points.Should().NotBeNull().And.BeEmpty();
    }
}
