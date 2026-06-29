using System.Security.Cryptography;
using System.Text;
using FitnessApp.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FitnessApp.Api.Controllers;

[Route("api/webhooks")]
[AllowAnonymous]
public class WebhooksController : ApiControllerBase
{
    private readonly DonationSettings _settings;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IOptions<DonationSettings> settings, ILogger<WebhooksController> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Buy Me a Coffee donation webhook. Returns 200 on an authentic call so BMAC does not
    /// retry. For now it only verifies the signature and logs the raw payload — once we
    /// capture a real "Send test" event we map it to a Donation entity and persist it.
    /// </summary>
    [HttpPost("buymeacoffee")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BuyMeACoffee()
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            rawBody = await reader.ReadToEndAsync();

        // BMAC has shipped two header names across webhook versions; accept either.
        var signature = FirstHeader("X-Signature-Sha256") ?? FirstHeader("X-Bmc-Signature");

        if (!string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            if (!IsSignatureValid(rawBody, signature, _settings.WebhookSecret))
            {
                _logger.LogWarning("BMAC webhook rejected: missing or invalid signature.");
                return Unauthorized();
            }
        }
        else
        {
            _logger.LogWarning(
                "BMAC webhook: no WebhookSecret configured — signature NOT verified (dev only).");
        }

        var eventType = FirstHeader("X-Bmc-Event");
        _logger.LogInformation(
            "BMAC webhook received. Event={Event} Signature={Signature} Payload={Payload}",
            eventType, signature, rawBody);

        return Ok();
    }

    private string? FirstHeader(string name) =>
        Request.Headers.TryGetValue(name, out var v) ? v.FirstOrDefault() : null;

    private static bool IsSignatureValid(string body, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
        var expected = signature.Trim().Replace("sha256=", "", StringComparison.OrdinalIgnoreCase);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(expected.ToLowerInvariant()));
    }
}
