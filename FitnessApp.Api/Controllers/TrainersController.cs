using System.Security.Claims;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Infrastructure.Persistence.AppDbContext _db;

    public TrainersController(UserManager<ApplicationUser> userManager, Infrastructure.Persistence.AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TrainerListItemDto>>> GetAll()
    {
        var trainers = await _userManager.GetUsersInRoleAsync(Roles.Trainer);
        return Ok(trainers
            .OrderBy(t => t.FullName ?? t.Email)
            .Select(t => new TrainerListItemDto
            {
                Id = t.Id,
                Email = t.Email!,
                FullName = t.FullName,
                ProfileImageUrl = t.ProfileImagePath
            }));
    }

    [HttpPost("me/clients")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<ClientListItemDto>> CreateClient(CreateClientRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "Korisnik s tim emailom već postoji." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true,
            TrainerId = UserId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, Roles.Client);

        return Ok(new ClientListItemDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            PlanCount = 0,
            PerformedSetCount = 0,
            ProfileImageUrl = user.ProfileImagePath
        });
    }

    [HttpGet("me/clients")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<ClientListItemDto>>> GetMyClients()
    {
        var clients = await _db.Users
            .Where(u => u.TrainerId == UserId)
            .OrderBy(u => u.FullName ?? u.Email)
            .Select(u => new ClientListItemDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                CreatedAt = u.CreatedAt,
                PlanCount = _db.TrainingPlans.Count(p => p.ClientId == u.Id),
                PerformedSetCount = _db.PerformedSets.Count(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == u.Id),
                ProfileImageUrl = u.ProfileImagePath
            })
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("me/clients/{clientId}/stats/my-exercises")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<TrainedExerciseDto>>> GetClientTrainedExercises(string clientId)
    {
        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound(new { message = "Klijent ne postoji." });
        if (client.TrainerId != UserId) return Forbid();

        var data = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == clientId)
            .Select(ps => ps.PlannedExercise.Exercise)
            .Distinct()
            .OrderBy(e => e.Name)
            .Select(e => new TrainedExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                MuscleGroup = e.MuscleGroup,
                Type = e.Type.ToString()
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("me/clients/{clientId}/stats/exercise-progress/{exerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<ExerciseProgressPointDto>>> GetClientExerciseProgress(string clientId, int exerciseId)
    {
        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound();
        if (client.TrainerId != UserId) return Forbid();

        var sets = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.ExerciseId == exerciseId
                         && ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == clientId)
            .Select(ps => new
            {
                ps.PerformedAt,
                ps.ActualReps,
                ps.ActualWeightKg
            })
            .ToListAsync();

        var data = sets
            .GroupBy(ps => ps.PerformedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ExerciseProgressPointDto
            {
                Date = g.Key,
                MaxWeight = g.Max(s => s.ActualWeightKg),
                TotalReps = g.Sum(s => s.ActualReps),
                TotalVolume = g.Sum(s => s.ActualWeightKg * s.ActualReps),
                SetCount = g.Count()
            })
            .ToList();

        return Ok(data);
    }

    [HttpGet("{trainerId}/profile")]
    [Authorize]
    public async Task<ActionResult<PersonalProfileDto>> GetTrainerProfile(string trainerId)
    {
        var trainer = await _userManager.FindByIdAsync(trainerId);
        if (trainer == null) return NotFound(new { message = "Trener ne postoji." });
        if (!await _userManager.IsInRoleAsync(trainer, Roles.Trainer))
            return BadRequest(new { message = "Korisnik nije trener." });

        return Ok(new PersonalProfileDto
        {
            FullName = trainer.FullName,
            Email = trainer.Email,
            BirthDate = trainer.BirthDate,
            Gender = trainer.Gender,
            HeightCm = trainer.HeightCm,
            WeightKg = trainer.WeightKg,
            Goal = trainer.Goal,
            HealthNotes = trainer.HealthNotes,
            ActivityLevel = trainer.ActivityLevel,
            Phone = trainer.Phone,
            PreferredWeeklyTrainingCount = trainer.PreferredWeeklyTrainingCount,
            PreferredTrainingType = trainer.PreferredTrainingType,
            ProfileImageUrl = trainer.ProfileImagePath,
            Role = Roles.Trainer
        });
    }

    [HttpGet("me/clients/{clientId}/profile")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<PersonalProfileDto>> GetClientProfile(string clientId)
    {
        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound(new { message = "Klijent ne postoji." });
        if (client.TrainerId != UserId) return Forbid();

        return Ok(new PersonalProfileDto
        {
            FullName = client.FullName,
            Email = client.Email,
            BirthDate = client.BirthDate,
            Gender = client.Gender,
            HeightCm = client.HeightCm,
            WeightKg = client.WeightKg,
            Goal = client.Goal,
            HealthNotes = client.HealthNotes,
            ActivityLevel = client.ActivityLevel,
            Phone = client.Phone,
            PreferredWeeklyTrainingCount = client.PreferredWeeklyTrainingCount,
            PreferredTrainingType = client.PreferredTrainingType,
            ProfileImageUrl = client.ProfileImagePath,
            Role = Roles.Client
        });
    }

    [HttpGet("me/clients/{clientId}/workouts")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IEnumerable<WorkoutListItemDto>>> GetClientWorkouts(string clientId)
    {
        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound();
        if (client.TrainerId != UserId) return Forbid();

        var data = await _db.Workouts
            .Where(w => w.ClientId == clientId && w.TrainerId == UserId)
            .OrderByDescending(w => w.PerformedAt)
            .Select(w => new WorkoutListItemDto
            {
                Id = w.Id,
                Name = w.Name,
                PerformedAt = w.PerformedAt,
                DurationMinutes = w.DurationMinutes,
                Notes = w.Notes,
                ExerciseCount = w.Exercises.Count
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost("me/clients/{clientId}/workouts")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<WorkoutDetailDto>> CreateClientWorkout(string clientId, CreateWorkoutRequest request)
    {
        var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == clientId);
        if (client == null) return NotFound();
        if (client.TrainerId != UserId) return Forbid();

        var workout = new Workout
        {
            ClientId = clientId,
            TrainerId = UserId,
            Name = request.Name,
            PerformedAt = request.PerformedAt ?? DateTime.UtcNow,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes
        };

        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        return Ok(MapWorkoutDetail(workout));
    }

    [HttpGet("me/clients/{clientId}/workouts/{workoutId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<WorkoutDetailDto>> GetClientWorkoutById(string clientId, int workoutId)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises).ThenInclude(we => we.Exercise)
            .Include(w => w.Exercises).ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == clientId);
        if (workout == null) return NotFound();
        if (workout.TrainerId != UserId) return Forbid();

        return Ok(MapWorkoutDetail(workout));
    }

    [HttpDelete("me/clients/{clientId}/workouts/{workoutId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteClientWorkout(string clientId, int workoutId)
    {
        var workout = await _db.Workouts
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == clientId);
        if (workout == null) return NotFound();
        if (workout.TrainerId != UserId) return Forbid();

        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("me/clients/{clientId}/workouts/{workoutId:int}/exercises")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> AddWorkoutExercise(string clientId, int workoutId, AddWorkoutExerciseRequest request)
    {
        var workout = await _db.Workouts.FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == clientId);
        if (workout == null) return NotFound();
        if (workout.TrainerId != UserId) return Forbid();

        var exercise = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == request.ExerciseId);
        if (exercise == null) return BadRequest(new { message = "Vježba ne postoji." });

        var entity = new WorkoutExercise
        {
            WorkoutId = workoutId,
            ExerciseId = request.ExerciseId,
            Order = request.Order
        };
        _db.WorkoutExercises.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { id = entity.Id });
    }

    [HttpPost("me/clients/{clientId}/workouts/{workoutId:int}/sets")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> AddWorkoutSet(string clientId, int workoutId, AddWorkoutSetRequest request)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == clientId);
        if (workout == null) return NotFound();
        if (workout.TrainerId != UserId) return Forbid();
        if (!workout.Exercises.Any(we => we.Id == request.WorkoutExerciseId))
            return BadRequest(new { message = "Vježba ne pripada ovom treningu." });

        var entity = new WorkoutSet
        {
            WorkoutExerciseId = request.WorkoutExerciseId,
            SetNumber = request.SetNumber,
            Weight = request.Weight,
            Reps = request.Reps
        };
        _db.WorkoutSets.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { id = entity.Id });
    }

    [HttpDelete("me/clients/{clientId}/sets/{setId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteWorkoutSet(string clientId, int setId)
    {
        var set = await _db.WorkoutSets
            .Include(s => s.WorkoutExercise).ThenInclude(we => we.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId);
        if (set == null) return NotFound();
        if (set.WorkoutExercise.Workout.ClientId != clientId) return BadRequest();
        if (set.WorkoutExercise.Workout.TrainerId != UserId) return Forbid();

        _db.WorkoutSets.Remove(set);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static WorkoutDetailDto MapWorkoutDetail(Workout w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        PerformedAt = w.PerformedAt,
        DurationMinutes = w.DurationMinutes,
        Notes = w.Notes,
        Exercises = w.Exercises
            .OrderBy(we => we.Order)
            .Select(we => new WorkoutExerciseDto
            {
                Id = we.Id,
                ExerciseId = we.ExerciseId,
                ExerciseName = we.Exercise?.Name ?? "",
                Order = we.Order,
                Sets = we.Sets
                    .OrderBy(s => s.SetNumber)
                    .Select(s => new WorkoutSetDto
                    {
                        Id = s.Id,
                        SetNumber = s.SetNumber,
                        Weight = s.Weight,
                        Reps = s.Reps,
                        Notes = s.Notes
                    }).ToList()
            }).ToList()
    };
}
