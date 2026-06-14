using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.Features.Stats.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class StatsController : ApiControllerBase
{
    private readonly ISender _sender;

    public StatsController(ISender sender) => _sender = sender;

    [HttpGet("exercise-progress/{exerciseId:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<ExerciseProgressPointDto>>> GetExerciseProgress(int exerciseId)
        => Ok(await _sender.Send(new GetExerciseProgressQuery(exerciseId)));

    [HttpGet("my-exercises")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<TrainedExerciseDto>>> GetTrainedExercises()
        => Ok(await _sender.Send(new GetTrainedExercisesQuery()));
}
