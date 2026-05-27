using System.Security.Claims;
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
[Authorize(Roles = Roles.Client)]
[Route("api/[controller]")]
public class WorkoutsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkoutsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutListItemDto>>> GetMyWorkouts()
    {
        var data = await _db.Workouts
            .Where(w => w.ClientId == UserId)
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

    [HttpPost]
    public async Task<ActionResult<WorkoutDetailDto>> Create(CreateWorkoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var me = await _userManager.FindByIdAsync(UserId);
        if (me == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(me.TrainerId))
            return BadRequest(new { message = "You need a trainer assigned to log workouts." });

        var workout = new Workout
        {
            ClientId = UserId,
            TrainerId = me.TrainerId,
            Name = request.Name,
            PerformedAt = request.PerformedAt ?? DateTime.UtcNow,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes
        };

        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        return Ok(MapDetail(workout));
    }

    [HttpGet("{workoutId:int}")]
    public async Task<ActionResult<WorkoutDetailDto>> GetById(int workoutId)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises).ThenInclude(we => we.Exercise)
            .Include(w => w.Exercises).ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == UserId);
        if (workout == null) return NotFound();

        return Ok(MapDetail(workout));
    }

    [HttpPost("{workoutId:int}/exercises")]
    public async Task<IActionResult> AddExercise(int workoutId, AddWorkoutExerciseRequest request)
    {
        var workout = await _db.Workouts
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == UserId);
        if (workout == null) return NotFound();

        var exists = await _db.Exercises.AnyAsync(e => e.Id == request.ExerciseId);
        if (!exists) return BadRequest(new { message = "Exercise not found." });

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

    [HttpDelete("exercises/{workoutExerciseId:int}")]
    public async Task<IActionResult> DeleteExercise(int workoutExerciseId)
    {
        var we = await _db.WorkoutExercises
            .Include(x => x.Workout)
            .FirstOrDefaultAsync(x => x.Id == workoutExerciseId);
        if (we == null) return NotFound();
        if (we.Workout.ClientId != UserId) return Forbid();

        _db.WorkoutExercises.Remove(we);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("exercises/{workoutExerciseId:int}/move")]
    public async Task<IActionResult> MoveExercise(int workoutExerciseId, [FromQuery] string direction)
    {
        if (direction != "up" && direction != "down")
            return BadRequest(new { message = "Direction must be 'up' or 'down'." });

        var we = await _db.WorkoutExercises
            .Include(x => x.Workout)
            .FirstOrDefaultAsync(x => x.Id == workoutExerciseId);
        if (we == null) return NotFound();
        if (we.Workout.ClientId != UserId) return Forbid();

        var siblings = await _db.WorkoutExercises
            .Where(x => x.WorkoutId == we.WorkoutId)
            .OrderBy(x => x.Order)
            .ToListAsync();

        var idx = siblings.FindIndex(x => x.Id == we.Id);
        var swapWith = direction == "up" ? idx - 1 : idx + 1;
        if (swapWith < 0 || swapWith >= siblings.Count) return NoContent();

        (we.Order, siblings[swapWith].Order) = (siblings[swapWith].Order, we.Order);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{workoutId:int}/sets")]
    public async Task<IActionResult> AddSet(int workoutId, AddWorkoutSetRequest request)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.ClientId == UserId);
        if (workout == null) return NotFound();
        if (!workout.Exercises.Any(we => we.Id == request.WorkoutExerciseId))
            return BadRequest(new { message = "Exercise does not belong to this workout." });

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

    [HttpDelete("sets/{setId:int}")]
    public async Task<IActionResult> DeleteSet(int setId)
    {
        var set = await _db.WorkoutSets
            .Include(s => s.WorkoutExercise).ThenInclude(we => we.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId);
        if (set == null) return NotFound();
        if (set.WorkoutExercise.Workout.ClientId != UserId) return Forbid();

        _db.WorkoutSets.Remove(set);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static WorkoutDetailDto MapDetail(Workout w) => new()
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
