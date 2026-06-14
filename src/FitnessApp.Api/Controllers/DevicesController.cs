using FitnessApp.Application.DTOs.Notifications;
using FitnessApp.Application.Features.Devices.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/devices")]
public class DevicesController : ApiControllerBase
{
    private readonly ISender _sender;

    public DevicesController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken ct)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _sender.Send(new RegisterDeviceCommand(request.Token, request.Platform, userAgent), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { saved = true });
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token, CancellationToken ct)
        => HandleResult(await _sender.Send(new UnregisterDeviceCommand(token), ct));

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        var result = await _sender.Send(new SendTestNotificationCommand(), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { sent = true });
    }
}
