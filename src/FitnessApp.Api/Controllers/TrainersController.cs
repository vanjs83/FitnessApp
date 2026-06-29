using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Application.Features.Trainers.Commands;
using FitnessApp.Application.Features.Trainers.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TrainersController : ApiControllerBase
{
    private readonly ISender _sender;

    public TrainersController(ISender sender) => _sender = sender;

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

    /// <summary>Public, paged directory of trainers.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType<PagedResult<TrainerListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TrainerListItemDto>>> GetAll([FromQuery] int page = 1, [FromQuery] string? search = null)
        => Ok(await _sender.Send(new GetAllTrainersQuery(page, search)));

    /// <summary>Trainer creates a client account.</summary>
    [HttpPost("me/clients")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<ClientListItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientListItemDto>> CreateClient(CreateClientRequest request)
        => HandleCreated(await _sender.Send(new CreateClientCommand(request.Email, request.FullName, request.Language, PublicBaseUrl())));

    /// <summary>The trainer's clients (paged).</summary>
    [HttpGet("me/clients")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<ClientListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientListItemDto>>> GetMyClients([FromQuery] int page = 1, [FromQuery] string? search = null)
        => Ok(await _sender.Send(new GetMyClientsQuery(page, search)));

    /// <summary>A client's trained exercises.</summary>
    [HttpGet("me/clients/{clientId}/stats/my-exercises")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<TrainedExerciseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TrainedExerciseDto>>> GetClientTrainedExercises(string clientId)
        => HandleResult(await _sender.Send(new GetClientTrainedExercisesQuery(clientId)));

    /// <summary>A client's progress photos.</summary>
    [HttpGet("me/clients/{clientId}/progress")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<ProgressPhotoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProgressPhotoDto>>> GetClientProgress(string clientId, [FromQuery] int? planId)
        => HandleResult(await _sender.Send(new GetClientProgressPhotosQuery(clientId, planId)));

    /// <summary>A client's progression for one exercise.</summary>
    [HttpGet("me/clients/{clientId}/stats/exercise-progress/{exerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<ExerciseProgressPointDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ExerciseProgressPointDto>>> GetClientExerciseProgress(string clientId, int exerciseId)
        => HandleResult(await _sender.Send(new GetClientExerciseProgressQuery(clientId, exerciseId)));

    /// <summary>A trainer's public profile.</summary>
    [HttpGet("{trainerId}/profile")]
    [Authorize]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType<PersonalProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalProfileDto>> GetTrainerProfile(string trainerId)
        => HandleResult(await _sender.Send(new GetTrainerProfileQuery(trainerId)));

    /// <summary>A client's profile.</summary>
    [HttpGet("me/clients/{clientId}/profile")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PersonalProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonalProfileDto>> GetClientProfile(string clientId)
        => HandleResult(await _sender.Send(new GetClientProfileQuery(clientId)));

    /// <summary>A client's workouts (paged).</summary>
    [HttpGet("me/clients/{clientId}/workouts")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<WorkoutListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<WorkoutListItemDto>>> GetClientWorkouts(string clientId, [FromQuery] int page = 1)
        => HandleResult(await _sender.Send(new GetClientWorkoutsQuery(clientId, page)));

    /// <summary>Trainer logs a workout for a client.</summary>
    [HttpPost("me/clients/{clientId}/workouts")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<WorkoutDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutDetailDto>> CreateClientWorkout(string clientId, CreateWorkoutRequest request)
        => HandleCreated(await _sender.Send(new CreateClientWorkoutCommand(
            clientId, request.Name, request.PerformedAt, request.DurationMinutes, request.Notes)));

    /// <summary>A client's workout by id.</summary>
    [HttpGet("me/clients/{clientId}/workouts/{workoutId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<WorkoutDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutDetailDto>> GetClientWorkoutById(string clientId, int workoutId)
        => HandleResult(await _sender.Send(new GetClientWorkoutByIdQuery(clientId, workoutId)));

    /// <summary>Delete a client's workout.</summary>
    [HttpDelete("me/clients/{clientId}/workouts/{workoutId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientWorkout(string clientId, int workoutId)
        => HandleResult(await _sender.Send(new DeleteClientWorkoutCommand(clientId, workoutId)));

    /// <summary>Remove an exercise from a client's workout.</summary>
    [HttpDelete("me/clients/{clientId}/workouts/{workoutId:int}/exercises/{workoutExerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkoutExercise(string clientId, int workoutId, int workoutExerciseId)
        => HandleResult(await _sender.Send(new DeleteClientWorkoutExerciseCommand(clientId, workoutId, workoutExerciseId)));

    /// <summary>Add an exercise to a client's workout.</summary>
    [HttpPost("me/clients/{clientId}/workouts/{workoutId:int}/exercises")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdResponse>> AddWorkoutExercise(string clientId, int workoutId, AddWorkoutExerciseRequest request)
        => HandleCreated(await _sender.Send(new AddClientWorkoutExerciseCommand(clientId, workoutId, request.ExerciseId, request.Order)));

    /// <summary>Add a set to a client's workout exercise.</summary>
    [HttpPost("me/clients/{clientId}/workouts/{workoutId:int}/sets")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdResponse>> AddWorkoutSet(string clientId, int workoutId, AddWorkoutSetRequest request)
        => HandleCreated(await _sender.Send(new AddClientWorkoutSetCommand(
            clientId, workoutId, request.WorkoutExerciseId, request.SetNumber, request.Weight, request.Reps)));

    /// <summary>Delete a set from a client's workout.</summary>
    [HttpDelete("me/clients/{clientId}/sets/{setId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkoutSet(string clientId, int setId)
        => HandleResult(await _sender.Send(new DeleteClientWorkoutSetCommand(clientId, setId)));
}
