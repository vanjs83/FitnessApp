using FitnessApp.Application.DTOs.Notifications;
using FitnessApp.Application.Features.Devices.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/devices")]
public class DevicesController : ApiControllerBase
{
    private readonly ISender _sender;

    public DevicesController(ISender sender) => _sender = sender;

    /// <summary>Register (or refresh) the caller's push device token.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken ct)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _sender.Send(new RegisterDeviceCommand(request.Token, request.Platform, userAgent), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { saved = true });
    }

    /// <summary>Remove a device token (e.g. on logout).</summary>
    [HttpDelete("{token}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unregister(string token, CancellationToken ct)
        => HandleResult(await _sender.Send(new UnregisterDeviceCommand(token), ct));

    /// <summary>Send a test push notification to the caller's devices.</summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        var result = await _sender.Send(new SendTestNotificationCommand(), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { sent = true });
    }
}
