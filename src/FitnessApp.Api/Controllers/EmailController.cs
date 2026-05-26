using System.Security.Claims;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Interfaces;
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
    private readonly IEmailService _email;

    public EmailController(AppDbContext db, IEmailService email)
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
        var (trainer, client, err) = await ResolveTrainerAndClientAsync(request.ClientId);
        if (err != null) return err;

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var trainerName = trainer!.FullName ?? trainer.Email!;
        var planLabel = (request.PlanType, lang) switch
        {
            ("nutrition", "en") => "nutrition plan",
            ("nutrition", _) => "plan prehrane",
            (_, "en") => "training plan",
            _ => "plan treninga"
        };
        var subject = lang == "en"
            ? $"Your new {planLabel} is ready — {request.PlanName}"
            : $"Tvoj novi {planLabel} je spreman — {request.PlanName}";
        var loginUrl = $"{Request.Scheme}://{Request.Host}/";

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: client!.Email!,
            subject: subject,
            templateKey: "plan-ready",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["ClientName"] = client.FullName ?? (lang == "en" ? "athlete" : "klijente"),
                ["TrainerName"] = trainerName,
                ["PlanLabel"] = planLabel,
                ["PlanName"] = request.PlanName,
                ["LoginUrl"] = loginUrl
            },
            replyTo: trainer.Email,
            replyToName: trainerName);

        if (!ok) return BadRequest(new { message = $"Sending failed: {error}" });
        return Ok(new { sent = true, to = client.Email });
    }

    [HttpPost("send-to-client")]
    public async Task<IActionResult> SendToClient(SendEmailToClientRequest request)
    {
        var (trainer, client, err) = await ResolveTrainerAndClientAsync(request.ClientId);
        if (err != null) return err;

        var lang = (request.Language ?? "hr").ToLowerInvariant();
        var trainerName = trainer!.FullName ?? trainer.Email!;

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: client!.Email!,
            subject: request.Subject,
            templateKey: "trainer-to-client",
            language: lang,
            placeholders: new Dictionary<string, string>
            {
                ["Body"] = request.Body,
                ["TrainerName"] = trainerName
            },
            replyTo: trainer.Email,
            replyToName: trainerName);

        if (!ok) return BadRequest(new { message = $"Sending failed: {error}" });
        return Ok(new { sent = true, to = client.Email, from = trainer.Email });
    }

    private async Task<(Infrastructure.Identity.ApplicationUser? trainer, Infrastructure.Identity.ApplicationUser? client, IActionResult? error)>
        ResolveTrainerAndClientAsync(string clientId)
    {
        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null)
            return (null, null, NotFound(new { message = "Client not found." }));
        if (client.TrainerId != UserId)
            return (null, null, Forbid());
        if (string.IsNullOrWhiteSpace(client.Email))
            return (null, null, BadRequest(new { message = "Client has no email address." }));

        var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
        if (string.IsNullOrWhiteSpace(trainer?.Email))
            return (null, null, BadRequest(new { message = "Your trainer profile has no email." }));

        return (trainer, client, null);
    }
}
