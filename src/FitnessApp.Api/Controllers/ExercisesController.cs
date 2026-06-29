using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Application.Features.Exercises.Commands;
using FitnessApp.Application.Features.Exercises.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExercisesController : ApiControllerBase
{
    private readonly ISender _sender;

    public ExercisesController(ISender sender) => _sender = sender;

    /// <summary>All exercises available to the caller.</summary>
    [HttpGet]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType<IReadOnlyList<ExerciseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExerciseDto>>> GetAll()
        => Ok(await _sender.Send(new GetExercisesQuery()));

    /// <summary>A single exercise.</summary>
    [HttpGet("{id:int}")]
    [ResponseCache(CacheProfileName = "Reference")]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetExerciseByIdQuery(id)));

    /// <summary>Create an exercise.</summary>
    [HttpPost]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExerciseDto>> Create(CreateExerciseRequest request)
    {
        var dto = await _sender.Send(new CreateExerciseCommand(
            request.Name, request.Description, request.VideoUrl, request.MuscleGroup, request.Type));
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Update an exercise.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> Update(int id, UpdateExerciseRequest request)
        => HandleResult(await _sender.Send(new UpdateExerciseCommand(
            id, request.Name, request.Description, request.VideoUrl, request.MuscleGroup, request.Type)));

    /// <summary>Delete an exercise.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteExerciseCommand(id)));

    /// <summary>Upload a demonstration video for an exercise.</summary>
    [HttpPost("{id:int}/video")]
    [RequestSizeLimit(UploadExerciseVideoCommandHandler.MaxVideoBytes + 1024)]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> UploadVideo(int id, IFormFile file)
        => HandleResult(await _sender.Send(new UploadExerciseVideoCommand(id, file)));

    /// <summary>Remove an exercise's video.</summary>
    [HttpDelete("{id:int}/video")]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> DeleteVideo(int id)
        => HandleResult(await _sender.Send(new DeleteExerciseVideoCommand(id)));

    /// <summary>Upload a thumbnail image for an exercise.</summary>
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(UploadExerciseImageCommandHandler.MaxImageBytes + 1024)]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> UploadImage(int id, IFormFile file)
        => HandleResult(await _sender.Send(new UploadExerciseImageCommand(id, file)));

    /// <summary>Remove an exercise's image.</summary>
    [HttpDelete("{id:int}/image")]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDto>> DeleteImage(int id)
        => HandleResult(await _sender.Send(new DeleteExerciseImageCommand(id)));
}
