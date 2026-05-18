using System.Security.Claims;
using FitnessApp.Api.Services;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Trainer)]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;

    public EmailController(AppDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("status")]
    public ActionResult<EmailStatusDto> GetStatus() =>
        Ok(new EmailStatusDto { Configured = _email.IsConfigured });

    [HttpPost("notify-plan-ready")]
    public async Task<IActionResult> NotifyPlanReady([FromBody] NotifyPlanReadyRequest request)
    {
        if (!_email.IsConfigured)
            return BadRequest(new { message = "SMTP is not configured in appsettings.json." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId);
        if (client == null) return NotFound(new { message = "Client not found." });
        if (client.TrainerId != UserId) return Forbid();
        if (string.IsNullOrWhiteSpace(client.Email))
            return BadRequest(new { message = "Client has no email address." });

        var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
        if (string.IsNullOrWhiteSpace(trainer?.Email))
            return BadRequest(new { message = "Your trainer profile has no email." });

        var trainerName = trainer.FullName ?? trainer.Email;
        var planLabel = request.PlanType == "nutrition" ? "plan prehrane" : "plan treninga";
        var subject = $"Tvoj novi {planLabel} je spreman — {request.PlanName}";
        var bodyText =
$@"Bok {client.FullName ?? "klijente"},

Tvoj novi {planLabel} ""{request.PlanName}"" je spreman u FitnessApp aplikaciji.

Prijavi se i pogledaj ga: https://fitnessapp.local/

Pozdrav,
{trainerName}";
        var htmlBody = WrapBody(bodyText, trainerName);

        try
        {
            await _email.SendAsync(
                toEmail: client.Email,
                subject: subject,
                body: htmlBody,
                fromEmail: trainer.Email,
                fromName: trainerName,
                replyTo: trainer.Email,
                replyToName: trainerName);
            return Ok(new { sent = true, to = client.Email });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Sending failed: {ex.Message}" });
        }
    }

    [HttpPost("send-to-client")]
    public async Task<IActionResult> SendToClient(SendEmailToClientRequest request)
    {
        if (!_email.IsConfigured)
            return BadRequest(new { message = "SMTP is not configured in appsettings.json." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId);
        if (client == null) return NotFound(new { message = "Client not found." });
        if (client.TrainerId != UserId) return Forbid();
        if (string.IsNullOrWhiteSpace(client.Email))
            return BadRequest(new { message = "Client has no email address." });

        var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
        if (string.IsNullOrWhiteSpace(trainer?.Email))
            return BadRequest(new { message = "Your trainer profile has no email." });

        var trainerName = trainer.FullName ?? trainer.Email;
        var htmlBody = WrapBody(request.Body, trainerName);

        try
        {
            await _email.SendAsync(
                toEmail: client.Email,
                subject: request.Subject,
                body: htmlBody,
                fromEmail: trainer.Email,
                fromName: trainerName,
                replyTo: trainer.Email,
                replyToName: trainerName);
            return Ok(new { sent = true, to = client.Email, from = trainer.Email });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Sending failed: {ex.Message}" });
        }
    }

    private static string WrapBody(string text, string trainerName)
    {
        var html = System.Net.WebUtility.HtmlEncode(text).Replace("\n", "<br>");
        return $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #222;'>
    <div style='border-bottom: 2px solid #4dabf7; padding-bottom: 12px; margin-bottom: 20px;'>
        <h2 style='margin: 0; color: #4dabf7;'>💪 FitnessApp</h2>
    </div>
    <div style='font-size: 15px; line-height: 1.6;'>{html}</div>
    <div style='margin-top: 30px; padding-top: 16px; border-top: 1px solid #eee; color: #888; font-size: 13px;'>
        Poslano od trenera <strong>{System.Net.WebUtility.HtmlEncode(trainerName)}</strong> preko FitnessApp platforme.
    </div>
</div>";
    }
}
