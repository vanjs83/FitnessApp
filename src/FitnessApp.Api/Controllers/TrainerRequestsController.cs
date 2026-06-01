using System.Security.Claims;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/trainer-requests")]
public class TrainerRequestsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<TrainerRequestsController> _logger;

    public TrainerRequestsController(
        UserManager<ApplicationUser> userManager,
        Infrastructure.Persistence.AppDbContext db,
        IEmailService email,
        ILogger<TrainerRequestsController> logger)
    {
        _userManager = userManager;
        _db = db;
        _email = email;
        _logger = logger;
    }

    // Public base URL for links in emails, derived from the current request's host.
    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ===== Client side =====

    [HttpPost]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<MyTrainerRequestDto>> SendRequest(CreateTrainerRequestRequest request)
    {
        var client = await _userManager.FindByIdAsync(UserId);
        if (client == null) return NotFound();
        if (!string.IsNullOrEmpty(client.TrainerId))
            return BadRequest(new { message = "You already have a trainer. Disconnect first before requesting another." });

        var trainer = await _userManager.FindByIdAsync(request.TrainerId);
        if (trainer == null || !await _userManager.IsInRoleAsync(trainer, Roles.Trainer))
            return BadRequest(new { message = "Selected trainer not found." });

        var hasPending = await _db.TrainerRequests
            .AnyAsync(r => r.ClientId == client.Id && r.Status == TrainerRequestStatus.Pending);
        if (hasPending)
            return BadRequest(new { message = "You already have a pending request. Cancel it first." });

        var entity = new TrainerRequest
        {
            ClientId = client.Id,
            TrainerId = trainer.Id,
            Status = TrainerRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.TrainerRequests.Add(entity);
        await _db.SaveChangesAsync();

        await TryNotifyTrainerAsync(trainer, client, request.Language);

        return Ok(new MyTrainerRequestDto
        {
            Id = entity.Id,
            TrainerId = trainer.Id,
            TrainerName = trainer.FullName ?? trainer.Email!,
            TrainerImageUrl = trainer.ProfileImagePath,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt,
            RespondedAt = entity.RespondedAt
        });
    }

    // Best-effort email to the trainer. Never blocks the request: if SMTP is not
    // configured or sending fails, we just log it and move on.
    private async Task TryNotifyTrainerAsync(ApplicationUser trainer, ApplicationUser client, string? language)
    {
        if (!_email.IsConfigured || string.IsNullOrWhiteSpace(trainer.Email))
            return;

        var lang = (language ?? "hr").ToLowerInvariant();
        var clientName = client.FullName ?? client.Email!;
        var subject = lang == "en"
            ? "New coaching request — FitnessApp"
            : "Novi zahtjev za suradnju — FitnessApp";

        try
        {
            var (ok, error) = await _email.SendTemplatedAsync(
                toEmail: trainer.Email!,
                subject: subject,
                templateKey: "trainer-request",
                language: lang,
                placeholders: new Dictionary<string, string>
                {
                    ["TrainerName"] = trainer.FullName ?? trainer.Email!,
                    ["ClientName"] = clientName,
                    ["LoginUrl"] = PublicBaseUrl() + "index.html"
                },
                replyTo: client.Email,
                replyToName: clientName);

            if (!ok)
                _logger.LogWarning("Trainer-request email to {Email} failed: {Error}", trainer.Email, error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trainer-request email to {Email} threw.", trainer.Email);
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<MyTrainerRequestDto>> GetMine()
    {
        var latest = await _db.TrainerRequests
            .Where(r => r.ClientId == UserId && r.Status != TrainerRequestStatus.Cancelled)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
        if (latest == null) return Ok((MyTrainerRequestDto?)null);

        var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == latest.TrainerId);

        return Ok(new MyTrainerRequestDto
        {
            Id = latest.Id,
            TrainerId = latest.TrainerId,
            TrainerName = trainer?.FullName ?? trainer?.Email ?? "",
            TrainerImageUrl = trainer?.ProfileImagePath,
            Status = latest.Status.ToString(),
            CreatedAt = latest.CreatedAt,
            RespondedAt = latest.RespondedAt
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> Cancel(int id)
    {
        var entity = await _db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null) return NotFound();
        if (entity.ClientId != UserId) return Forbid();
        if (entity.Status != TrainerRequestStatus.Pending)
            return BadRequest(new { message = "Only a pending request can be cancelled." });

        entity.Status = TrainerRequestStatus.Cancelled;
        entity.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Trainer side =====

    [HttpGet("incoming")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<IncomingTrainerRequestDto>>> GetIncoming()
    {
        var items = await _db.TrainerRequests
            .Where(r => r.TrainerId == UserId && r.Status == TrainerRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Join(_db.Users,
                r => r.ClientId,
                u => u.Id,
                (r, u) => new IncomingTrainerRequestDto
                {
                    Id = r.Id,
                    ClientId = u.Id,
                    ClientName = u.FullName ?? u.Email!,
                    ClientEmail = u.Email!,
                    ClientImageUrl = u.ProfileImagePath,
                    CreatedAt = r.CreatedAt
                })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Accept(int id)
    {
        var entity = await _db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null) return NotFound();
        if (entity.TrainerId != UserId) return Forbid();
        if (entity.Status != TrainerRequestStatus.Pending)
            return BadRequest(new { message = "Request is no longer pending." });

        var client = await _userManager.FindByIdAsync(entity.ClientId);
        if (client == null) return BadRequest(new { message = "Client not found." });
        if (!string.IsNullOrEmpty(client.TrainerId))
            return BadRequest(new { message = "Client already has a trainer." });

        client.TrainerId = UserId;
        var result = await _userManager.UpdateAsync(client);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        entity.Status = TrainerRequestStatus.Accepted;
        entity.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { clientId = client.Id });
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Reject(int id)
    {
        var entity = await _db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null) return NotFound();
        if (entity.TrainerId != UserId) return Forbid();
        if (entity.Status != TrainerRequestStatus.Pending)
            return BadRequest(new { message = "Request is no longer pending." });

        entity.Status = TrainerRequestStatus.Rejected;
        entity.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
