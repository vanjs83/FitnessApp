using System.Security.Claims;
using FitnessApp.Application.DTOs.Notifications;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPushNotificationService _push;

    public DevicesController(AppDbContext db, IPushNotificationService push)
    {
        _db = db;
        _push = push;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Token is required." });

        var userAgent = Request.Headers.UserAgent.ToString();
        if (userAgent.Length > 500) userAgent = userAgent[..500];

        var existing = await _db.Devices.FirstOrDefaultAsync(t => t.Token == request.Token, ct);
        if (existing is null)
        {
            _db.Devices.Add(new Device
            {
                UserId = UserId,
                Token = request.Token,
                Platform = string.IsNullOrWhiteSpace(request.Platform) ? "web" : request.Platform,
                UserAgent = userAgent,
                IsActive = true
            });
        }
        else
        {
            existing.UserId = UserId;
            existing.Platform = string.IsNullOrWhiteSpace(request.Platform) ? existing.Platform : request.Platform;
            existing.UserAgent = userAgent;
            existing.LastSeenAt = DateTime.UtcNow;
            existing.IsActive = true;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { saved = true });
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token, CancellationToken ct)
    {
        var entity = await _db.Devices.FirstOrDefaultAsync(t => t.Token == token && t.UserId == UserId, ct);
        if (entity is null) return NotFound();

        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        await _push.SendToUserAsync(UserId, "FitnessApp test", "Push notifikacije rade!", ct: ct);
        return Ok(new { sent = true });
    }
}
