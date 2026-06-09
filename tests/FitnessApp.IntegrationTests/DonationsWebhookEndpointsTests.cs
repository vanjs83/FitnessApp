using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FitnessApp.IntegrationTests;

/// <summary>
/// Covers the Buy Me a Coffee webhook endpoint (POST /api/webhooks/buymeacoffee):
/// signature verification when a secret is configured, and the dev fallback when it isn't.
/// The endpoint must answer 200 on an authentic call so BMAC does not retry.
/// </summary>
public class DonationsWebhookEndpointsTests : IntegrationTestBase
{
    private const string TestSecret = "test-webhook-secret-123";
    private const string SamplePayload = """{"type":"donation.created","data":{"amount":5}}""";

    public DonationsWebhookEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    /// <summary>A client whose app has a known webhook secret configured, so we can sign payloads.</summary>
    private HttpClient ClientWithSecret() =>
        Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Donations:WebhookSecret"] = TestSecret
                }))).CreateClient();

    private static string Sign(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }

    private static HttpRequestMessage WebhookRequest(string payload, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/buymeacoffee")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        if (signature is not null)
            request.Headers.Add("X-Signature-Sha256", signature);
        return request;
    }

    [Fact]
    public async Task Webhook_with_a_valid_signature_returns_200()
    {
        var client = ClientWithSecret();
        var signature = Sign(SamplePayload, TestSecret);

        var response = await client.SendAsync(WebhookRequest(SamplePayload, signature));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_with_an_invalid_signature_returns_401()
    {
        var client = ClientWithSecret();

        var response = await client.SendAsync(WebhookRequest(SamplePayload, "deadbeef"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_with_a_signature_for_the_wrong_body_returns_401()
    {
        var client = ClientWithSecret();
        // Sign a different body than the one we send — a tampered payload must be rejected.
        var signature = Sign("""{"type":"donation.created","data":{"amount":9999}}""", TestSecret);

        var response = await client.SendAsync(WebhookRequest(SamplePayload, signature));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_without_a_signature_returns_401_when_secret_is_configured()
    {
        var client = ClientWithSecret();

        var response = await client.SendAsync(WebhookRequest(SamplePayload, signature: null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_accepts_any_payload_with_200_when_no_secret_is_configured()
    {
        // The default test app has an empty Donations:WebhookSecret, so verification is
        // skipped (dev fallback) and an unsigned call still succeeds.
        var client = Factory.CreateClient();

        var response = await client.SendAsync(WebhookRequest(SamplePayload, signature: null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
