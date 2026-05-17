using System.Security.Claims;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly Infrastructure.Persistence.AppDbContext _db;

    public StatsController(Infrastructure.Persistence.AppDbContext db)
    {
        _db = db;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("exercise-progress/{exerciseId:int}")]
    public async Task<ActionResult<IEnumerable<ExerciseProgressPointDto>>> GetExerciseProgress(int exerciseId)
    {
        var sets = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.ExerciseId == exerciseId
                         && ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == UserId)
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

    [HttpGet("my-exercises")]
    public async Task<ActionResult<IEnumerable<TrainedExerciseDto>>> GetTrainedExercises()
    {
        var data = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == UserId)
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
}
