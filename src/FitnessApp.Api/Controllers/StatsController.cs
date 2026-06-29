using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.Features.Stats.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StatsController : ApiControllerBase
{
    private readonly ISender _sender;

    public StatsController(ISender sender) => _sender = sender;

    /// <summary>Weight/volume progression points for one exercise the caller has trained.</summary>
    [HttpGet("exercise-progress/{exerciseId:int}")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<ExerciseProgressPointDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExerciseProgressPointDto>>> GetExerciseProgress(int exerciseId)
        => Ok(await _sender.Send(new GetExerciseProgressQuery(exerciseId)));

    /// <summary>The distinct exercises the caller has performed.</summary>
    [HttpGet("my-exercises")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<TrainedExerciseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrainedExerciseDto>>> GetTrainedExercises()
        => Ok(await _sender.Send(new GetTrainedExercisesQuery()));
}
