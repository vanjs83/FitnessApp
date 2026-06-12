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
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Route("api/[controller]")]
public class TrainersController : ApiControllerBase
{
    private readonly ISender _sender;

    public TrainersController(ISender sender) => _sender = sender;

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(CacheProfileName = "Reference")]
    public async Task<ActionResult<IReadOnlyList<TrainerListItemDto>>> GetAll()
        => Ok(await _sender.Send(new GetAllTrainersQuery()));

    [HttpPost("me/clients")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<ClientListItemDto>> CreateClient(CreateClientRequest request)
        => HandleResult(await _sender.Send(new CreateClientCommand(request.Email, request.FullName, request.Language, PublicBaseUrl())));

    [HttpGet("me/clients")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<ClientListItemDto>>> GetMyClients()
        => Ok(await _sender.Send(new GetMyClientsQuery()));

    [HttpGet("me/clients/{clientId}/stats/my-exercises")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<TrainedExerciseDto>>> GetClientTrainedExercises(string clientId)
        => HandleResult(await _sender.Send(new GetClientTrainedExercisesQuery(clientId)));

    [HttpGet("me/clients/{clientId}/progress")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<ProgressPhotoDto>>> GetClientProgress(string clientId)
        => HandleResult(await _sender.Send(new GetClientProgressPhotosQuery(clientId)));

    [HttpGet("me/clients/{clientId}/stats/exercise-progress/{exerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<ExerciseProgressPointDto>>> GetClientExerciseProgress(string clientId, int exerciseId)
        => HandleResult(await _sender.Send(new GetClientExerciseProgressQuery(clientId, exerciseId)));

    [HttpGet("{trainerId}/profile")]
    [Authorize]
    [ResponseCache(CacheProfileName = "Reference")]
    public async Task<ActionResult<PersonalProfileDto>> GetTrainerProfile(string trainerId)
        => HandleResult(await _sender.Send(new GetTrainerProfileQuery(trainerId)));

    [HttpGet("me/clients/{clientId}/profile")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<PersonalProfileDto>> GetClientProfile(string clientId)
        => HandleResult(await _sender.Send(new GetClientProfileQuery(clientId)));

    [HttpGet("me/clients/{clientId}/workouts")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<WorkoutListItemDto>>> GetClientWorkouts(string clientId)
        => HandleResult(await _sender.Send(new GetClientWorkoutsQuery(clientId)));

    [HttpPost("me/clients/{clientId}/workouts")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<WorkoutDetailDto>> CreateClientWorkout(string clientId, CreateWorkoutRequest request)
        => HandleResult(await _sender.Send(new CreateClientWorkoutCommand(
            clientId, request.Name, request.PerformedAt, request.DurationMinutes, request.Notes)));

    [HttpGet("me/clients/{clientId}/workouts/{workoutId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<WorkoutDetailDto>> GetClientWorkoutById(string clientId, int workoutId)
        => HandleResult(await _sender.Send(new GetClientWorkoutByIdQuery(clientId, workoutId)));

    [HttpDelete("me/clients/{clientId}/workouts/{workoutId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteClientWorkout(string clientId, int workoutId)
        => HandleResult(await _sender.Send(new DeleteClientWorkoutCommand(clientId, workoutId)));

    [HttpDelete("me/clients/{clientId}/workouts/{workoutId:int}/exercises/{workoutExerciseId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteWorkoutExercise(string clientId, int workoutId, int workoutExerciseId)
        => HandleResult(await _sender.Send(new DeleteClientWorkoutExerciseCommand(clientId, workoutId, workoutExerciseId)));

    [HttpPost("me/clients/{clientId}/workouts/{workoutId:int}/exercises")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IdResponse>> AddWorkoutExercise(string clientId, int workoutId, AddWorkoutExerciseRequest request)
        => HandleResult(await _sender.Send(new AddClientWorkoutExerciseCommand(clientId, workoutId, request.ExerciseId, request.Order)));

    [HttpPost("me/clients/{clientId}/workouts/{workoutId:int}/sets")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<IdResponse>> AddWorkoutSet(string clientId, int workoutId, AddWorkoutSetRequest request)
        => HandleResult(await _sender.Send(new AddClientWorkoutSetCommand(
            clientId, workoutId, request.WorkoutExerciseId, request.SetNumber, request.Weight, request.Reps)));

    [HttpDelete("me/clients/{clientId}/sets/{setId:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> DeleteWorkoutSet(string clientId, int setId)
        => HandleResult(await _sender.Send(new DeleteClientWorkoutSetCommand(clientId, setId)));
}
