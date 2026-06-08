using System.Net;
using System.Net.Http.Json;
using FitnessApp.Application.DTOs.Chat;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.DTOs.Support;
using FluentAssertions;
using Xunit;

namespace FitnessApp.IntegrationTests;

/// <summary>
/// Low-setup coverage for the smaller controllers: each test still runs through the
/// full pipeline (auth, MediatR, EF) — just without complex data fixtures.
/// </summary>
public class MiscEndpointsTests : IntegrationTestBase
{
    public MiscEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ===== Chat =====

    [Fact]
    public async Task Chat_without_a_token_returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/chat/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_new_user_has_no_conversations()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var conversations = await client.GetFromJsonAsync<List<ConversationDto>>(
            "/api/chat/conversations", JsonOptions);

        conversations.Should().NotBeNull().And.BeEmpty();
    }

    // ===== Progress (client-only) =====

    [Fact]
    public async Task A_client_starts_with_no_progress_photos()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var photos = await client.GetFromJsonAsync<List<ProgressPhotoDto>>("/api/progress", JsonOptions);

        photos.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task A_trainer_cannot_access_client_progress_returns_403()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var response = await trainer.GetAsync("/api/progress");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ===== Email (trainer-only) =====

    [Fact]
    public async Task A_trainer_can_read_the_email_status()
    {
        var trainer = await CreateAuthenticatedClientAsync("Trainer");

        var status = await trainer.GetFromJsonAsync<EmailStatusDto>("/api/email/status", JsonOptions);

        status.Should().NotBeNull();
    }

    [Fact]
    public async Task A_client_cannot_read_the_email_status_returns_403()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var response = await client.GetAsync("/api/email/status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ===== Support =====

    [Fact]
    public async Task An_authenticated_user_can_read_the_support_status()
    {
        var client = await CreateAuthenticatedClientAsync("Client");

        var status = await client.GetFromJsonAsync<SupportStatusDto>("/api/support/status", JsonOptions);

        status.Should().NotBeNull();
    }
}
