using FitnessApp.Api.Data;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
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

    public AdminController(UserManager<ApplicationUser> userManager, Infrastructure.Persistence.AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

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
            return BadRequest(new { message = "Korisnik s tim emailom već postoji." });

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
        await DbSeeder.SeedDefaultExercisesForTrainerAsync(_db, user.Id);

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
            return BadRequest(new { message = "Korisnik nije trener." });

        var clients = await _db.Users.Where(u => u.TrainerId == id).ToListAsync();
        foreach (var c in clients)
            c.TrainerId = null;

        trainer.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
