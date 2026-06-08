using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Application.DTOs.TrainingPlans;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class PlansAuthorizationTests : IntegrationTestBase
{
    public PlansAuthorizationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ===== Training plans =====

    [Fact]
    public async Task Training_plans_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/training-plans/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_client_cannot_create_a_training_plan_returns_403()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.PostAsJsonAsync("/api/training-plans", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_new_trainer_has_no_training_plans()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var plans = await trainer.GetFromJsonAsync<List<TrainingPlanListItemDto>>(
            "/api/training-plans/mine", JsonOptions);

        plans.Should().NotBeNull().And.BeEmpty();
    }

    // ===== Nutrition plans =====

    [Fact]
    public async Task A_client_cannot_create_a_nutrition_plan_returns_403()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.PostAsJsonAsync("/api/nutrition-plans", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_new_trainer_has_no_nutrition_plans()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var plans = await trainer.GetFromJsonAsync<List<NutritionPlanListItemDto>>(
            "/api/nutrition-plans/mine", JsonOptions);

        plans.Should().NotBeNull().And.BeEmpty();
    }
}
