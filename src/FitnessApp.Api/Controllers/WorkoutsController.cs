using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Application.Features.Workouts.Commands;
using FitnessApp.Application.Features.Workouts.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.Client)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WorkoutsController : ApiControllerBase
{
    private readonly ISender _sender;

    public WorkoutsController(ISender sender) => _sender = sender;

    /// <summary>The client's workouts (paged).</summary>
    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<WorkoutListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkoutListItemDto>>> GetMyWorkouts([FromQuery] int page = 1)
        => Ok(await _sender.Send(new GetMyWorkoutsQuery(page)));

    /// <summary>Log a new workout.</summary>
    [HttpPost]
    [ProducesResponseType<WorkoutDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkoutDetailDto>> Create(CreateWorkoutRequest request)
        => HandleCreated(await _sender.Send(new CreateWorkoutCommand(
            request.Name, request.PerformedAt, request.DurationMinutes, request.Notes)));

    /// <summary>A single workout with its exercises and sets.</summary>
    [HttpGet("{workoutId:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<WorkoutDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutDetailDto>> GetById(int workoutId)
        => HandleResult(await _sender.Send(new GetWorkoutByIdQuery(workoutId)));

    /// <summary>Add an exercise to a workout.</summary>
    [HttpPost("{workoutId:int}/exercises")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdResponse>> AddExercise(int workoutId, AddWorkoutExerciseRequest request)
        => HandleCreated(await _sender.Send(new AddWorkoutExerciseCommand(workoutId, request.ExerciseId, request.Order)));

    /// <summary>Remove an exercise from a workout.</summary>
    [HttpDelete("exercises/{workoutExerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExercise(int workoutExerciseId)
        => HandleResult(await _sender.Send(new DeleteWorkoutExerciseCommand(workoutExerciseId)));

    /// <summary>Reorder an exercise within its workout.</summary>
    [HttpPut("exercises/{workoutExerciseId:int}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveExercise(int workoutExerciseId, [FromQuery] string direction)
        => HandleResult(await _sender.Send(new MoveWorkoutExerciseCommand(workoutExerciseId, direction)));

    /// <summary>Add a set to a workout exercise.</summary>
    [HttpPost("{workoutId:int}/sets")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdResponse>> AddSet(int workoutId, AddWorkoutSetRequest request)
        => HandleCreated(await _sender.Send(new AddWorkoutSetCommand(
            workoutId, request.WorkoutExerciseId, request.SetNumber, request.Weight, request.Reps)));

    /// <summary>Remove a set.</summary>
    [HttpDelete("sets/{setId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSet(int setId)
        => HandleResult(await _sender.Send(new DeleteWorkoutSetCommand(setId)));
}
