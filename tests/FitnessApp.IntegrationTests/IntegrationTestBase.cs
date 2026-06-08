using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.DTOs.Trainers;
using Xunit;

namespace FitnessApp.IntegrationTests;

/// <summary>
/// Shared setup for the integration tests: ensures the schema/roles exist and
/// provides helpers to register a user and get an authenticated <see cref="HttpClient"/>.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;

    /// <summary>
    /// Matches the API's serialization: web defaults (camelCase) plus enums as strings,
    /// so responses like "type":"Strength" deserialize correctly in the tests.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Factory.InitializeDatabase();
    }

    /// <summary>A unique email so tests stay independent even when they share a database.</summary>
    protected static string UniqueEmail() => $"user_{Guid.NewGuid():N}@test.local";

    /// <summary>Registers a fresh user and returns a client whose requests carry their JWT.</summary>
    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string role = "Trainer")
        => (await RegisterUserAsync(role)).Client;

    /// <summary>
    /// Registers a fresh user and returns an authenticated client plus their identity.
    /// The id is read back from /api/auth/me (the register response doesn't carry it).
    /// </summary>
    protected async Task<(HttpClient Client, string UserId, string Email)> RegisterUserAsync(string role)
    {
        var client = Factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test123!",
            fullName = $"{role} User",
            role
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me", JsonOptions);
        return (client, me!.Id, email);
    }

    /// <summary>
    /// Registers a client and a trainer, then runs the real request→accept flow so the
    /// client ends up assigned to the trainer. Returns both authenticated clients and ids.
    /// </summary>
    protected async Task<(HttpClient Client, string ClientId, HttpClient Trainer, string TrainerId)> CreateLinkedClientAndTrainerAsync()
    {
        var (client, clientId, _) = await RegisterUserAsync("Client");
        var (trainer, trainerId, _) = await RegisterUserAsync("Trainer");

        var send = await client.PostAsJsonAsync("/api/trainer-requests",
            new CreateTrainerRequestRequest { TrainerId = trainerId });
        send.EnsureSuccessStatusCode();

        var incoming = await trainer.GetFromJsonAsync<List<IncomingTrainerRequestDto>>(
            "/api/trainer-requests/incoming", JsonOptions);
        var requestId = incoming!.Single().Id;

        var accept = await trainer.PostAsync($"/api/trainer-requests/{requestId}/accept", null);
        accept.EnsureSuccessStatusCode();

        return (client, clientId, trainer, trainerId);
    }
}
