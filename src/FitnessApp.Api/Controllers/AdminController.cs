using System.Security.Claims;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.SuperAdmin)]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly IEmailService _email;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        Infrastructure.Persistence.AppDbContext db,
        IEmailService email)
    {
        _userManager = userManager;
        _db = db;
        _email = email;
    }

    private string AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("trainers")]
    public async Task<ActionResult<IEnumerable<TrainerAdminDto>>> GetTrainers()
    {
        var trainers = await _userManager.GetUsersInRoleAsync(Roles.Trainer);
        var ids = trainers.Select(t => t.Id).ToList();

        var clientCounts = await _db.Users
            .Where(u => u.TrainerId != null && ids.Contains(u.TrainerId!))
            .GroupBy(u => u.TrainerId!)
            .Select(g => new { TrainerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TrainerId, x => x.Count);

        var result = trainers
            .OrderBy(t => t.FullName ?? t.Email)
            .Select(t => new TrainerAdminDto
            {
                Id = t.Id,
                Email = t.Email!,
                FullName = t.FullName,
                CreatedAt = t.CreatedAt,
                ClientCount = clientCounts.TryGetValue(t.Id, out var c) ? c : 0
            });

        return Ok(result);
    }

    [HttpPost("trainers")]
    public async Task<ActionResult<TrainerAdminDto>> CreateTrainer(CreateTrainerRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "A user with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, Roles.Trainer);

        return Ok(new TrainerAdminDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            ClientCount = 0
        });
    }

    [HttpGet("clients")]
    public async Task<ActionResult<IEnumerable<ClientAdminDto>>> GetClients()
    {
        var clientUsers = await _userManager.GetUsersInRoleAsync(Roles.Client);
        var clientIds = clientUsers.Select(c => c.Id).ToList();

        var performedCounts = await _db.PerformedSets
            .Where(ps => clientIds.Contains(ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId))
            .GroupBy(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count);

        var trainerIds = clientUsers
            .Where(c => c.TrainerId != null)
            .Select(c => c.TrainerId!)
            .Distinct()
            .ToList();

        var trainers = await _db.Users
            .Where(u => trainerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email!);

        var result = clientUsers
            .OrderBy(c => c.FullName ?? c.Email)
            .Select(c => new ClientAdminDto
            {
                Id = c.Id,
                Email = c.Email!,
                FullName = c.FullName,
                CreatedAt = c.CreatedAt,
                TrainerName = c.TrainerId != null && trainers.TryGetValue(c.TrainerId, out var tn) ? tn : null,
                PerformedSetCount = performedCounts.TryGetValue(c.Id, out var pc) ? pc : 0
            });

        return Ok(result);
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IEnumerable<PlanAdminDto>>> GetPlans()
    {
        var plans = await _db.TrainingPlans
            .OrderByDescending(p => p.StartDate)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                DayCount = p.Days.Count,
                PerformedSetCount = p.Days.SelectMany(d => d.Exercises).SelectMany(pe => pe.PerformedSets).Count(),
                p.TrainerId,
                p.ClientId
            })
            .ToListAsync();

        var userIds = plans.Select(p => p.TrainerId).Concat(plans.Select(p => p.ClientId)).Distinct().ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email!);

        var result = plans.Select(p => new PlanAdminDto
        {
            Id = p.Id,
            Name = p.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            DayCount = p.DayCount,
            PerformedSetCount = p.PerformedSetCount,
            ClientName = users.TryGetValue(p.ClientId, out var cn) ? cn : "(nepoznat)",
            TrainerName = users.TryGetValue(p.TrainerId, out var tn) ? tn : null
        });

        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStats()
    {
        var trainers = await _userManager.GetUsersInRoleAsync(Roles.Trainer);
        var clients = await _userManager.GetUsersInRoleAsync(Roles.Client);

        return Ok(new AdminStatsDto
        {
            TrainersCount = trainers.Count,
            ClientsCount = clients.Count,
            ClientsWithoutTrainer = clients.Count(c => c.TrainerId == null),
            PlansCount = await _db.TrainingPlans.CountAsync(),
            PerformedSetsCount = await _db.PerformedSets.CountAsync(),
            ExercisesCount = await _db.Exercises.CountAsync()
        });
    }

    [HttpDelete("trainers/{id}")]
    public async Task<IActionResult> DeleteTrainer(string id)
    {
        var trainer = await _userManager.FindByIdAsync(id);
        if (trainer == null) return NotFound();
        if (!await _userManager.IsInRoleAsync(trainer, Roles.Trainer))
            return BadRequest(new { message = "User is not a trainer." });

        var clients = await _db.Users.Where(u => u.TrainerId == id).ToListAsync();
        foreach (var c in clients)
            c.TrainerId = null;

        trainer.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("email/status")]
    public ActionResult<EmailStatusDto> GetEmailStatus() =>
        Ok(new EmailStatusDto { Configured = _email.IsConfigured });

    [HttpPost("email/send-to-trainers")]
    public async Task<ActionResult<EmailSendResultDto>> SendToTrainers(SendEmailToTrainersRequest request)
    {
        var admin = await _userManager.FindByIdAsync(AdminId);
        if (admin == null || string.IsNullOrWhiteSpace(admin.Email))
            return BadRequest(new { message = "Administrator profil nema email." });

        var adminName = admin.FullName ?? admin.Email!;
        var lang = (request.Language ?? "hr").ToLowerInvariant();

        var trainers = await _userManager.GetUsersInRoleAsync(Roles.Trainer);
        var trainersById = trainers.ToDictionary(t => t.Id);

        var result = new EmailSendResultDto();

        foreach (var trainerId in request.TrainerIds.Distinct())
        {
            if (!trainersById.TryGetValue(trainerId, out var trainer))
            {
                result.Failed.Add(new EmailFailureDto
                {
                    TrainerId = trainerId,
                    Email = string.Empty,
                    Error = "Trener nije pronađen ili nema rolu Trainer."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(trainer.Email))
            {
                result.Failed.Add(new EmailFailureDto
                {
                    TrainerId = trainerId,
                    Email = string.Empty,
                    Error = "Trener nema email adresu."
                });
                continue;
            }

            var (ok, error) = await _email.SendTemplatedAsync(
                toEmail: trainer.Email!,
                subject: request.Subject,
                templateKey: "admin-to-trainer",
                language: lang,
                placeholders: new Dictionary<string, string>
                {
                    ["TrainerName"] = trainer.FullName ?? trainer.Email!,
                    ["AdminName"] = adminName,
                    ["Subject"] = request.Subject,
                    ["Body"] = request.Body
                },
                replyTo: admin.Email,
                replyToName: adminName);

            if (ok)
                result.Sent.Add(trainer.Email!);
            else
                result.Failed.Add(new EmailFailureDto
                {
                    TrainerId = trainerId,
                    Email = trainer.Email!,
                    Error = error ?? "Slanje neuspjelo."
                });
        }

        return Ok(result);
    }
}
