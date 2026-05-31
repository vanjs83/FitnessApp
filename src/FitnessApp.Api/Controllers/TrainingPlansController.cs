using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Claims;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/training-plans")]
public class TrainingPlansController : ControllerBase
{
    private const string ShareKind = "training";

    private readonly AppDbContext _db;
    private readonly ITrainingPlanPdfService _pdfService;
    private readonly IPlanShareTokenService _shareTokens;

    public TrainingPlansController(AppDbContext db, ITrainingPlanPdfService pdfService, IPlanShareTokenService shareTokens)
    {
        _db = db;
        _pdfService = pdfService;
        _shareTokens = shareTokens;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsTrainer => User.IsInRole(Roles.Trainer);

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<TrainingPlanListItemDto>>> GetMyPlans()
    {
        var plans = await _db.TrainingPlans
            .Where(p => p.TrainerId == UserId && !p.IsTemplate)
            .OrderByDescending(p => p.StartDate)
            .Join(_db.Users,
                p => p.ClientId,
                u => u.Id,
                (p, u) => new TrainingPlanListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ClientId = p.ClientId!,
                    ClientName = u.FullName ?? u.Email!,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    DayCount = p.Days.Count,
                    Price = p.Price,
                    Currency = p.Currency,
                    PaymentStatus = p.PaymentStatus
                })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IEnumerable<TrainingPlanListItemDto>>> GetForClient(string clientId)
    {
        if (!IsTrainer && clientId != UserId) return Forbid();

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound();

        if (IsTrainer && client.TrainerId != UserId) return Forbid();

        var query = _db.TrainingPlans.Where(p => p.ClientId == clientId && !p.IsTemplate);
        if (!IsTrainer)
        {
            if (string.IsNullOrEmpty(client.TrainerId))
                return Ok(Array.Empty<TrainingPlanListItemDto>());
            query = query.Where(p => p.TrainerId == client.TrainerId);
        }

        var plans = await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new TrainingPlanListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                ClientId = p.ClientId!,
                ClientName = client.FullName ?? client.Email!,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DayCount = p.Days.Count,
                Price = p.Price,
                Currency = p.Currency,
                PaymentStatus = p.PaymentStatus
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingPlanDetailDto>> GetById(int id)
    {
        var plan = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Completions)
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.PerformedSets)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound();

        if (plan.IsTemplate)
        {
            if (!IsTrainer || plan.TrainerId != UserId) return Forbid();
            return Ok(MapDetail(plan, "", false));
        }

        if (IsTrainer && plan.TrainerId != UserId) return Forbid();
        if (!IsTrainer && plan.ClientId != UserId) return Forbid();

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == plan.ClientId);
        if (!IsTrainer && client?.TrainerId != plan.TrainerId) return Forbid();

        var isLocked = !IsTrainer && plan.PaymentStatus != PaymentStatus.Approved;
        return Ok(MapDetail(plan, client?.FullName ?? client?.Email ?? "", isLocked));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> Create(CreateTrainingPlanRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId);
        if (client == null) return BadRequest(new { message = "Client not found." });
        if (client.TrainerId != UserId) return Forbid();

        var plan = new TrainingPlan
        {
            TrainerId = UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TrainerExpectations = request.TrainerExpectations,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved
        };
        if (plan.PaymentStatus == PaymentStatus.Approved)
            plan.ApprovedAt = DateTime.UtcNow;

        _db.TrainingPlans.Add(plan);
        await _db.SaveChangesAsync();

        return Ok(MapDetail(plan, client.FullName ?? client.Email ?? "", false));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Update(int id, UpdateTrainingPlanRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        plan.Name = request.Name;
        plan.StartDate = request.StartDate;
        plan.EndDate = request.EndDate;
        plan.TrainerExpectations = request.TrainerExpectations;
        plan.Price = request.Price;
        plan.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency;

        if (plan.Price <= 0 && plan.PaymentStatus != PaymentStatus.Approved)
        {
            plan.PaymentStatus = PaymentStatus.Approved;
            plan.ApprovedAt ??= DateTime.UtcNow;
        }
        else if (plan.Price > 0 && plan.PaymentStatus == PaymentStatus.Approved && plan.ApprovedAt == null)
        {
            plan.PaymentStatus = PaymentStatus.Pending;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/claim-payment")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> ClaimPayment(int id)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.ClientId != UserId) return Forbid();

        if (plan.PaymentStatus == PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is already approved." });

        plan.PaymentStatus = PaymentStatus.PaymentClaimed;
        plan.PaymentClaimedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { paymentStatus = plan.PaymentStatus.ToString(), paymentClaimedAt = plan.PaymentClaimedAt });
    }

    [HttpPost("{id:int}/approve-payment")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> ApprovePayment(int id)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        plan.PaymentStatus = PaymentStatus.Approved;
        plan.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { paymentStatus = plan.PaymentStatus.ToString(), approvedAt = plan.ApprovedAt });
    }

    [HttpPost("{id:int}/revoke-approval")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> RevokeApproval(int id)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        if (plan.Price <= 0)
            return BadRequest(new { message = "A free plan cannot be locked." });

        plan.PaymentStatus = PaymentStatus.Pending;
        plan.ApprovedAt = null;
        plan.PaymentClaimedAt = null;
        await _db.SaveChangesAsync();

        return Ok(new { paymentStatus = plan.PaymentStatus.ToString() });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Delete(int id)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        _db.TrainingPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Templates =====

    [HttpGet("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<TrainingPlanTemplateListItemDto>>> GetMyTemplates()
    {
        var items = await _db.TrainingPlans
            .Where(p => p.TrainerId == UserId && p.IsTemplate)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TrainingPlanTemplateListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                TrainerExpectations = p.TrainerExpectations,
                DayCount = p.Days.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("templates")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> CreateTemplate(CreateTrainingPlanTemplateRequest request)
    {
        var template = new TrainingPlan
        {
            TrainerId = UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            TrainerExpectations = request.TrainerExpectations,
            Price = 0,
            Currency = "EUR",
            PaymentStatus = PaymentStatus.Approved
        };
        _db.TrainingPlans.Add(template);
        await _db.SaveChangesAsync();

        return Ok(MapDetail(template, "", false));
    }

    [HttpPut("templates/{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> UpdateTemplate(int id, UpdateTrainingPlanTemplateRequest request)
    {
        var template = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (template == null) return NotFound();
        if (template.TrainerId != UserId) return Forbid();
        if (!template.IsTemplate) return BadRequest(new { message = "Not a template." });

        template.Name = request.Name;
        template.TrainerExpectations = request.TrainerExpectations;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("templates/{templateId:int}/clone")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> CloneTemplateToClient(int templateId, CloneTemplateToClientRequest request)
    {
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        var template = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == templateId);
        if (template == null) return NotFound();
        if (template.TrainerId != UserId) return Forbid();
        if (!template.IsTemplate) return BadRequest(new { message = "Plan is not a template." });

        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.ClientId);
        if (client == null) return BadRequest(new { message = "Client not found." });
        if (client.TrainerId != UserId) return Forbid();

        var newPlan = new TrainingPlan
        {
            TrainerId = UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            IsTemplate = false,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TrainerExpectations = request.TrainerExpectations ?? template.TrainerExpectations,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved
        };
        if (newPlan.PaymentStatus == PaymentStatus.Approved)
            newPlan.ApprovedAt = DateTime.UtcNow;

        foreach (var d in template.Days.OrderBy(x => x.DayOfWeek))
        {
            var newDay = new TrainingDay
            {
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                Notes = d.Notes
            };
            foreach (var pe in d.Exercises.OrderBy(x => x.Order))
            {
                newDay.Exercises.Add(new PlannedExercise
                {
                    ExerciseId = pe.ExerciseId,
                    Order = pe.Order,
                    TargetSets = pe.TargetSets,
                    TargetReps = pe.TargetReps,
                    TargetWeightKg = pe.TargetWeightKg,
                    TargetDurationSeconds = pe.TargetDurationSeconds,
                    RestSeconds = pe.RestSeconds,
                    Notes = pe.Notes
                });
            }
            newPlan.Days.Add(newDay);
        }

        _db.TrainingPlans.Add(newPlan);
        await _db.SaveChangesAsync();

        return Ok(MapDetail(newPlan, client.FullName ?? client.Email ?? "", false));
    }

    [HttpPost("{planId:int}/save-as-template")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingPlanDetailDto>> SaveAsTemplate(int planId, CreateTrainingPlanTemplateRequest request)
    {
        var source = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (source == null) return NotFound();
        if (source.TrainerId != UserId) return Forbid();

        var template = new TrainingPlan
        {
            TrainerId = UserId,
            ClientId = null,
            Name = request.Name,
            IsTemplate = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            TrainerExpectations = request.TrainerExpectations ?? source.TrainerExpectations,
            Price = 0,
            Currency = "EUR",
            PaymentStatus = PaymentStatus.Approved
        };

        foreach (var d in source.Days.OrderBy(x => x.DayOfWeek))
        {
            var newDay = new TrainingDay
            {
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                Notes = d.Notes
            };
            foreach (var pe in d.Exercises.OrderBy(x => x.Order))
            {
                newDay.Exercises.Add(new PlannedExercise
                {
                    ExerciseId = pe.ExerciseId,
                    Order = pe.Order,
                    TargetSets = pe.TargetSets,
                    TargetReps = pe.TargetReps,
                    TargetWeightKg = pe.TargetWeightKg,
                    TargetDurationSeconds = pe.TargetDurationSeconds,
                    RestSeconds = pe.RestSeconds,
                    Notes = pe.Notes
                });
            }
            template.Days.Add(newDay);
        }

        _db.TrainingPlans.Add(template);
        await _db.SaveChangesAsync();
        return Ok(MapDetail(template, "", false));
    }

    [HttpPost("{planId:int}/days")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<TrainingDayDto>> AddDay(int planId, AddTrainingDayRequest request)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return NotFound();
        if (plan.TrainerId != UserId) return Forbid();

        var day = new TrainingDay
        {
            TrainingPlanId = planId,
            DayOfWeek = request.DayOfWeek,
            Label = request.Label,
            Notes = request.Notes
        };

        _db.TrainingDays.Add(day);
        await _db.SaveChangesAsync();

        return Ok(new TrainingDayDto
        {
            Id = day.Id,
            DayOfWeek = day.DayOfWeek,
            Label = day.Label,
            Notes = day.Notes,
            Exercises = new()
        });
    }

    [HttpDelete("days/{dayId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteDay(int dayId)
    {
        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .FirstOrDefaultAsync(d => d.Id == dayId);
        if (day == null) return NotFound();
        if (day.TrainingPlan.TrainerId != UserId) return Forbid();

        _db.TrainingDays.Remove(day);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("days/{dayId:int}/exercises")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PlannedExerciseDto>> AddPlannedExercise(int dayId, AddPlannedExerciseRequest request)
    {
        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .FirstOrDefaultAsync(d => d.Id == dayId);
        if (day == null) return NotFound();
        if (day.TrainingPlan.TrainerId != UserId) return Forbid();

        var exercise = await _db.Exercises.FindAsync(request.ExerciseId);
        if (exercise == null) return BadRequest(new { message = "Exercise not found." });
        if (exercise.CreatedByUserId != UserId)
            return BadRequest(new { message = "You can only use your own exercises." });

        if (request.TargetReps <= 0 && !request.TargetDurationSeconds.HasValue)
            return BadRequest(new { message = "Exercise must have either reps or duration." });

        var pe = new PlannedExercise
        {
            TrainingDayId = dayId,
            ExerciseId = request.ExerciseId,
            Order = request.Order,
            TargetSets = request.TargetSets,
            TargetReps = request.TargetReps,
            TargetWeightKg = request.TargetWeightKg,
            TargetDurationSeconds = request.TargetDurationSeconds,
            RestSeconds = request.RestSeconds,
            Notes = request.Notes
        };

        _db.PlannedExercises.Add(pe);
        await _db.SaveChangesAsync();

        return Ok(new PlannedExerciseDto
        {
            Id = pe.Id,
            ExerciseId = pe.ExerciseId,
            ExerciseName = exercise.Name,
            Order = pe.Order,
            TargetSets = pe.TargetSets,
            TargetReps = pe.TargetReps,
            TargetWeightKg = pe.TargetWeightKg,
            TargetDurationSeconds = pe.TargetDurationSeconds,
            RestSeconds = pe.RestSeconds,
            Notes = pe.Notes
        });
    }

    [HttpPut("exercises/{plannedExerciseId:int}/move")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> MovePlannedExercise(int plannedExerciseId, [FromQuery] string direction)
    {
        if (direction != "up" && direction != "down")
            return BadRequest(new { message = "direction mora biti 'up' ili 'down'." });

        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == plannedExerciseId);
        if (pe == null) return NotFound();
        if (pe.TrainingDay.TrainingPlan.TrainerId != UserId) return Forbid();

        var siblings = await _db.PlannedExercises
            .Where(x => x.TrainingDayId == pe.TrainingDayId)
            .OrderBy(x => x.Order)
            .ToListAsync();

        var idx = siblings.FindIndex(x => x.Id == pe.Id);
        var swapIdx = direction == "up" ? idx - 1 : idx + 1;
        if (swapIdx < 0 || swapIdx >= siblings.Count) return NoContent();

        var other = siblings[swapIdx];
        (pe.Order, other.Order) = (other.Order, pe.Order);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("exercises/{plannedExerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeletePlannedExercise(int plannedExerciseId)
    {
        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == plannedExerciseId);
        if (pe == null) return NotFound();
        if (pe.TrainingDay.TrainingPlan.TrainerId != UserId) return Forbid();

        _db.PlannedExercises.Remove(pe);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("days/{dayId:int}/toggle-today")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<object>> ToggleDayCompletion(int dayId)
    {
        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .Include(d => d.Exercises).ThenInclude(pe => pe.Completions)
            .FirstOrDefaultAsync(d => d.Id == dayId);
        if (day == null) return NotFound();
        if (day.TrainingPlan.ClientId != UserId) return Forbid();
        if (day.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is not approved." });
        if (!day.Exercises.Any())
            return BadRequest(new { message = "The day has no exercises to mark." });

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        bool IsCompletedToday(PlannedExercise pe) =>
            pe.Completions.Any(c => c.ClientId == UserId && c.CompletedAt >= today && c.CompletedAt < tomorrow);

        var allCompleted = day.Exercises.All(IsCompletedToday);

        if (allCompleted)
        {
            var toRemove = day.Exercises
                .SelectMany(pe => pe.Completions
                    .Where(c => c.ClientId == UserId && c.CompletedAt >= today && c.CompletedAt < tomorrow))
                .ToList();
            _db.PlannedExerciseCompletions.RemoveRange(toRemove);
            await _db.SaveChangesAsync();
            return Ok(new { isCompletedToday = false });
        }

        foreach (var pe in day.Exercises.Where(p => !IsCompletedToday(p)))
        {
            _db.PlannedExerciseCompletions.Add(new PlannedExerciseCompletion
            {
                PlannedExerciseId = pe.Id,
                ClientId = UserId,
                CompletedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        return Ok(new { isCompletedToday = true });
    }

    [HttpPost("exercises/{plannedExerciseId:int}/toggle-today")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<object>> ToggleTodayCompletion(int plannedExerciseId)
    {
        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == plannedExerciseId);
        if (pe == null) return NotFound();
        if (pe.TrainingDay.TrainingPlan.ClientId != UserId) return Forbid();
        if (pe.TrainingDay.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is not approved." });

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var existing = await _db.PlannedExerciseCompletions
            .FirstOrDefaultAsync(c => c.PlannedExerciseId == plannedExerciseId
                                       && c.ClientId == UserId
                                       && c.CompletedAt >= today
                                       && c.CompletedAt < tomorrow);

        if (existing != null)
        {
            _db.PlannedExerciseCompletions.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { isCompletedToday = false });
        }

        _db.PlannedExerciseCompletions.Add(new PlannedExerciseCompletion
        {
            PlannedExerciseId = plannedExerciseId,
            ClientId = UserId,
            CompletedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { isCompletedToday = true });
    }

    [HttpGet("exercises/{plannedExerciseId:int}/performed-sets")]
    public async Task<ActionResult<IEnumerable<PerformedSetDto>>> GetPerformedSets(int plannedExerciseId)
    {
        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == plannedExerciseId);
        if (pe == null) return NotFound();

        var plan = pe.TrainingDay.TrainingPlan;
        if (IsTrainer && plan.TrainerId != UserId) return Forbid();
        if (!IsTrainer && plan.ClientId != UserId) return Forbid();

        var sets = await _db.PerformedSets
            .Where(ps => ps.PlannedExerciseId == plannedExerciseId)
            .OrderByDescending(ps => ps.PerformedAt)
            .ThenByDescending(ps => ps.SetNumber)
            .Select(ps => new PerformedSetDto
            {
                Id = ps.Id,
                PlannedExerciseId = ps.PlannedExerciseId,
                SetNumber = ps.SetNumber,
                ActualReps = ps.ActualReps,
                ActualWeightKg = ps.ActualWeightKg,
                PerformedAt = ps.PerformedAt,
                Notes = ps.Notes
            })
            .ToListAsync();

        return Ok(sets);
    }

    [HttpPost("exercises/{plannedExerciseId:int}/performed-sets")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<PerformedSetDto>> LogPerformedSet(int plannedExerciseId, LogPerformedSetRequest request)
    {
        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == plannedExerciseId);
        if (pe == null) return NotFound();
        if (pe.TrainingDay.TrainingPlan.ClientId != UserId) return Forbid();
        if (pe.TrainingDay.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is not approved." });

        var entity = new PerformedSet
        {
            PlannedExerciseId = plannedExerciseId,
            SetNumber = request.SetNumber,
            ActualReps = request.ActualReps,
            ActualWeightKg = request.ActualWeightKg,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Notes
        };
        _db.PerformedSets.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new PerformedSetDto
        {
            Id = entity.Id,
            PlannedExerciseId = entity.PlannedExerciseId,
            SetNumber = entity.SetNumber,
            ActualReps = entity.ActualReps,
            ActualWeightKg = entity.ActualWeightKg,
            PerformedAt = entity.PerformedAt,
            Notes = entity.Notes
        });
    }

    [HttpDelete("performed-sets/{id:int}")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> DeletePerformedSet(int id)
    {
        var ps = await _db.PerformedSets
            .Include(s => s.PlannedExercise).ThenInclude(pe => pe.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (ps == null) return NotFound();
        if (ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId != UserId) return Forbid();
        if (ps.PlannedExercise.TrainingDay.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is not approved." });

        _db.PerformedSets.Remove(ps);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/weight-progression")]
    public async Task<ActionResult<IEnumerable<PlanExerciseProgressionDto>>> GetWeightProgression(int id)
    {
        var plan = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        if (IsTrainer && plan.TrainerId != UserId) return Forbid();
        if (!IsTrainer && plan.ClientId != UserId) return Forbid();
        if (!IsTrainer && plan.PaymentStatus != PaymentStatus.Approved)
            return Ok(Array.Empty<PlanExerciseProgressionDto>());

        var rangeStart = plan.StartDate.Date;
        var rangeEnd = plan.EndDate.Date.AddDays(1);
        var clientId = plan.ClientId;

        var planned = plan.Days
            .SelectMany(d => d.Exercises)
            .GroupBy(pe => pe.ExerciseId)
            .Select(g => new
            {
                ExerciseId = g.Key,
                ExerciseName = g.First().Exercise?.Name ?? "",
                MuscleGroup = g.First().Exercise?.MuscleGroup,
                TargetWeightKg = g.Max(x => x.TargetWeightKg),
                TargetSets = g.Max(x => x.TargetSets),
                TargetReps = g.Max(x => x.TargetReps),
                PlannedExerciseIds = g.Select(x => x.Id).ToList()
            })
            .OrderBy(x => x.ExerciseName)
            .ToList();

        if (planned.Count == 0)
            return Ok(Array.Empty<PlanExerciseProgressionDto>());

        var allPlannedIds = planned.SelectMany(p => p.PlannedExerciseIds).ToList();

        var rawSets = await _db.PerformedSets
            .Where(ps => allPlannedIds.Contains(ps.PlannedExerciseId)
                         && ps.PerformedAt >= rangeStart
                         && ps.PerformedAt < rangeEnd)
            .Select(ps => new
            {
                ps.PlannedExerciseId,
                ps.PerformedAt,
                ps.ActualReps,
                ps.ActualWeightKg
            })
            .ToListAsync();

        var plannedIdToExerciseId = planned
            .SelectMany(p => p.PlannedExerciseIds.Select(pid => new { pid, p.ExerciseId }))
            .ToDictionary(x => x.pid, x => x.ExerciseId);

        var pointsByExercise = rawSets
            .GroupBy(s => new { ExerciseId = plannedIdToExerciseId[s.PlannedExerciseId], Date = s.PerformedAt.Date })
            .Select(g => new
            {
                g.Key.ExerciseId,
                Point = new ExerciseProgressPointDto
                {
                    Date = g.Key.Date,
                    MaxWeight = g.Max(s => s.ActualWeightKg),
                    TotalReps = g.Sum(s => s.ActualReps),
                    TotalVolume = g.Sum(s => s.ActualWeightKg * s.ActualReps),
                    SetCount = g.Count()
                }
            })
            .GroupBy(x => x.ExerciseId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Point).OrderBy(p => p.Date).ToList());

        var result = planned.Select(p => new PlanExerciseProgressionDto
        {
            ExerciseId = p.ExerciseId,
            ExerciseName = p.ExerciseName,
            MuscleGroup = p.MuscleGroup,
            TargetWeightKg = p.TargetWeightKg,
            TargetSets = p.TargetSets,
            TargetReps = p.TargetReps,
            Points = pointsByExercise.TryGetValue(p.ExerciseId, out var pts) ? pts : new List<ExerciseProgressPointDto>()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var plan = await LoadPlanForExport(id);
        if (plan == null) return NotFound();

        if (IsTrainer && plan.TrainerId != UserId) return Forbid();
        if (!IsTrainer && plan.ClientId != UserId) return Forbid();
        if (!IsTrainer && plan.PaymentStatus != PaymentStatus.Approved)
            return BadRequest(new { message = "Plan is not approved." });

        var bytes = await BuildPdfBytes(plan);
        return File(bytes, "application/pdf", PdfFileName(plan));
    }

    [HttpPost("{id:int}/share")]
    public async Task<ActionResult<object>> CreateShareLink(int id)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();

        if (IsTrainer)
        {
            if (plan.TrainerId != UserId) return Forbid();
        }
        else
        {
            if (plan.ClientId != UserId) return Forbid();
            if (plan.PaymentStatus != PaymentStatus.Approved)
                return BadRequest(new { message = "Plan is not approved." });
        }

        var token = _shareTokens.CreateForKind(ShareKind, plan.Id);
        var baseUrl = BuildPublicBaseUrl();
        var shareUrl = $"{baseUrl}/api/training-plans/share/{token}/pdf";

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(shareUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(20);

        return Ok(new
        {
            url = shareUrl,
            qrPngBase64 = Convert.ToBase64String(pngBytes),
            expiresInHours = 24
        });
    }

    [HttpGet("share/{token}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadSharedPdf(string token)
    {
        if (!_shareTokens.TryValidateForKind(ShareKind, token, out var planId))
            return NotFound(new { message = "Link expired or invalid." });

        var plan = await LoadPlanForExport(planId);
        if (plan == null) return NotFound();
        if (plan.PaymentStatus != PaymentStatus.Approved)
            return NotFound(new { message = "Plan is not available." });

        var bytes = await BuildPdfBytes(plan);
        return File(bytes, "application/pdf", PdfFileName(plan));
    }

    private async Task<TrainingPlan?> LoadPlanForExport(int id)
    {
        return await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<byte[]> BuildPdfBytes(TrainingPlan plan)
    {
        var users = await _db.Users
            .Where(u => u.Id == plan.ClientId || u.Id == plan.TrainerId)
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.Email!);

        users.TryGetValue(plan.ClientId, out var clientName);
        users.TryGetValue(plan.TrainerId, out var trainerName);

        return _pdfService.Generate(plan, clientName, trainerName);
    }

    private string BuildPublicBaseUrl()
    {
        var host = Request.Host.Host;
        var port = Request.Host.Port;
        var scheme = Request.Scheme;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "::1"
            || host == "0.0.0.0")
        {
            var lanIp = GetLocalLanIp();
            if (lanIp != null) host = lanIp;
        }

        return port.HasValue ? $"{scheme}://{host}:{port}" : $"{scheme}://{host}";
    }

    private static string? GetLocalLanIp()
    {
        try
        {
            var candidates = new List<string>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var desc = (ni.Description ?? "") + " " + (ni.Name ?? "");
                if (desc.Contains("WSL", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("VMware", StringComparison.OrdinalIgnoreCase)
                    || desc.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase))
                    continue;
                var ipProps = ni.GetIPProperties();
                if (ipProps.GatewayAddresses.Count == 0) continue;
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(addr.Address))
                    {
                        candidates.Add(addr.Address.ToString());
                    }
                }
            }

            return candidates
                .OrderBy(ip => ip.StartsWith("192.168.") ? 0
                            : ip.StartsWith("10.") ? 1
                            : 2)
                .FirstOrDefault();
        }
        catch { /* ignore — fall back to caller default */ }
        return null;
    }

    private static string PdfFileName(TrainingPlan plan)
    {
        var safeName = string.Concat(plan.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "plan";
        return $"{safeName}_{plan.StartDate:yyyyMMdd}.pdf";
    }

    private static TrainingPlanDetailDto MapDetail(TrainingPlan p, string clientName, bool isLocked)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return new TrainingPlanDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            ClientId = p.ClientId,
            ClientName = clientName,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            TrainerExpectations = isLocked ? null : p.TrainerExpectations,
            Price = p.Price,
            Currency = p.Currency,
            PaymentStatus = p.PaymentStatus,
            PaymentClaimedAt = p.PaymentClaimedAt,
            ApprovedAt = p.ApprovedAt,
            IsTemplate = p.IsTemplate,
            IsLocked = isLocked,
            Days = isLocked ? new() : p.Days
                .OrderBy(d => d.DayOfWeek)
                .Select(d => new TrainingDayDto
                {
                    Id = d.Id,
                    DayOfWeek = d.DayOfWeek,
                    Label = d.Label,
                    Notes = d.Notes,
                    IsCompletedToday = d.Exercises.Any() && d.Exercises.All(e =>
                        e.Completions.Any(c => c.CompletedAt >= today && c.CompletedAt < tomorrow)),
                    Exercises = d.Exercises
                        .OrderBy(e => e.Order)
                        .Select(e => new PlannedExerciseDto
                        {
                            Id = e.Id,
                            ExerciseId = e.ExerciseId,
                            ExerciseName = e.Exercise?.Name ?? "",
                            Order = e.Order,
                            TargetSets = e.TargetSets,
                            TargetReps = e.TargetReps,
                            TargetWeightKg = e.TargetWeightKg,
                            TargetDurationSeconds = e.TargetDurationSeconds,
                            RestSeconds = e.RestSeconds,
                            Notes = e.Notes,
                            CompletionsCount = e.Completions.Count,
                            IsCompletedToday = e.Completions.Any(c => c.CompletedAt >= today && c.CompletedAt < tomorrow),
                            LastCompletedAt = e.Completions.OrderByDescending(c => c.CompletedAt).FirstOrDefault()?.CompletedAt,
                            RecentPerformedSets = e.PerformedSets
                                .OrderByDescending(ps => ps.PerformedAt)
                                .ThenByDescending(ps => ps.SetNumber)
                                .Take(20)
                                .Select(ps => new PerformedSetDto
                                {
                                    Id = ps.Id,
                                    PlannedExerciseId = ps.PlannedExerciseId,
                                    SetNumber = ps.SetNumber,
                                    ActualReps = ps.ActualReps,
                                    ActualWeightKg = ps.ActualWeightKg,
                                    PerformedAt = ps.PerformedAt,
                                    Notes = ps.Notes
                                }).ToList()
                        }).ToList()
                }).ToList()
        };
    }
}
