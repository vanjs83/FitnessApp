using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Notifications;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class DevicesEndpointsTests : IntegrationTestBase
{
    public DevicesEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Registering_a_device_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/devices",
            new RegisterDeviceRequest { Token = "abc", Platform = "web" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_authenticated_user_can_register_a_device()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.PostAsJsonAsync("/api/devices",
            new RegisterDeviceRequest { Token = $"token-{Guid.NewGuid():N}", Platform = "web" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Registering_a_device_with_an_empty_token_returns_400()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.PostAsJsonAsync("/api/devices",
            new RegisterDeviceRequest { Token = "", Platform = "web" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
