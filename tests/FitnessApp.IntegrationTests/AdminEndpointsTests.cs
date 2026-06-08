using System.Net;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class AdminEndpointsTests : IntegrationTestBase
{
    public AdminEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Admin_endpoints_without_a_token_return_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/admin/trainers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_client_cannot_access_admin_endpoints_returns_403()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.GetAsync("/api/admin/trainers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_trainer_cannot_access_admin_endpoints_returns_403()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var response = await trainer.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
