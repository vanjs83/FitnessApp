using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Application.Features.Workouts.Commands;
using FitnessApp.Application.Features.Workouts.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.Client)]
[Route("api/v{version:apiVersion}/[controller]")]
public class WorkoutsController : ApiControllerBase
{
    private readonly ISender _sender;

    public WorkoutsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<WorkoutListItemDto>>> GetMyWorkouts()
        => Ok(await _sender.Send(new GetMyWorkoutsQuery()));

    [HttpPost]
    public async Task<ActionResult<WorkoutDetailDto>> Create(CreateWorkoutRequest request)
        => HandleResult(await _sender.Send(new CreateWorkoutCommand(
            request.Name, request.PerformedAt, request.DurationMinutes, request.Notes)));

    [HttpGet("{workoutId:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<WorkoutDetailDto>> GetById(int workoutId)
        => HandleResult(await _sender.Send(new GetWorkoutByIdQuery(workoutId)));

    [HttpPost("{workoutId:int}/exercises")]
    public async Task<ActionResult<IdResponse>> AddExercise(int workoutId, AddWorkoutExerciseRequest request)
        => HandleResult(await _sender.Send(new AddWorkoutExerciseCommand(workoutId, request.ExerciseId, request.Order)));

    [HttpDelete("exercises/{workoutExerciseId:int}")]
    public async Task<IActionResult> DeleteExercise(int workoutExerciseId)
        => HandleResult(await _sender.Send(new DeleteWorkoutExerciseCommand(workoutExerciseId)));

    [HttpPut("exercises/{workoutExerciseId:int}/move")]
    public async Task<IActionResult> MoveExercise(int workoutExerciseId, [FromQuery] string direction)
        => HandleResult(await _sender.Send(new MoveWorkoutExerciseCommand(workoutExerciseId, direction)));

    [HttpPost("{workoutId:int}/sets")]
    public async Task<ActionResult<IdResponse>> AddSet(int workoutId, AddWorkoutSetRequest request)
        => HandleResult(await _sender.Send(new AddWorkoutSetCommand(
            workoutId, request.WorkoutExerciseId, request.SetNumber, request.Weight, request.Reps)));

    [HttpDelete("sets/{setId:int}")]
    public async Task<IActionResult> DeleteSet(int setId)
        => HandleResult(await _sender.Send(new DeleteWorkoutSetCommand(setId)));
}
