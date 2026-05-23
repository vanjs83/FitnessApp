using System.Security.Claims;
using FitnessApp.Application.DTOs.Notifications;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Trainer)]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPushNotificationService _push;

    public NotificationsController(AppDbContext db, IPushNotificationService push)
    {
        _db = db;
        _push = push;
    }

    private string TrainerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("notify-client-plan")]
    public async Task<IActionResult> NotifyClientPlan(NotifyClientPlanRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.PlanName))
            return BadRequest(new { message = "ClientId and PlanName are required." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId, ct);
        if (client is null) return NotFound(new { message = "Client not found." });
        if (client.TrainerId != TrainerId) return Forbid();

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var (title, body) = (request.PlanType, lang) switch
        {
            ("nutrition", "en") => ("Nutrition plan ready", $"Your new nutrition plan '{request.PlanName}' is ready."),
            ("nutrition", _)    => ("Plan prehrane spreman", $"Tvoj novi plan prehrane '{request.PlanName}' je spreman."),
            (_, "en")           => ("Training plan ready", $"Your new training plan '{request.PlanName}' is ready."),
            _                   => ("Plan treninga spreman", $"Tvoj novi plan treninga '{request.PlanName}' je spreman.")
        };

        await _push.SendToUserAsync(client.Id, title, body, new Dictionary<string, string>
        {
            ["planType"] = request.PlanType,
            ["planName"] = request.PlanName
        }, ct);

        var activeTokens = await _db.Devices.CountAsync(t => t.UserId == client.Id && t.IsActive, ct);
        return Ok(new { sent = true, activeTokens });
    }
}
