using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Auth;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

public class RefreshTokenEndpointsTests : IntegrationTestBase
{
    public RefreshTokenEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    /// <summary>Registers a fresh user and returns the issued token pair.</summary>
    private async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = UniqueEmail(),
            password = "Test123!",
            fullName = "Refresh User",
            role = "Client"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task Register_issues_a_refresh_token()
    {
        var client = Factory.CreateClient();

        var auth = await RegisterAsync(client);

        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Refresh_returns_a_new_rotated_token_pair()
    {
        var client = Factory.CreateClient();
        var auth = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.RefreshToken });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed.Should().NotBeNull();
        refreshed!.Token.Should().NotBeNullOrWhiteSpace();
        // Rotation: a brand-new refresh token, not the one we sent.
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task Old_refresh_token_is_rejected_after_rotation()
    {
        var client = Factory.CreateClient();
        var auth = await RegisterAsync(client);

        var first = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.RefreshToken });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Replaying the original (now rotated-out) token must fail.
        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.RefreshToken });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var client = Factory.CreateClient();
        var auth = await RegisterAsync(client);

        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = auth.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_an_unknown_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "not-a-real-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_an_empty_token_returns_400()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
