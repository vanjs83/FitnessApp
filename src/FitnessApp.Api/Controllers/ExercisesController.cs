using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Application.Features.Exercises.Commands;
using FitnessApp.Application.Features.Exercises.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ExercisesController : ApiControllerBase
{
    private readonly ISender _sender;

    public ExercisesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExerciseDto>>> GetAll()
        => Ok(await _sender.Send(new GetExercisesQuery()));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetExerciseByIdQuery(id)));

    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> Create(CreateExerciseRequest request)
    {
        var dto = await _sender.Send(new CreateExerciseCommand(
            request.Name, request.Description, request.VideoUrl, request.MuscleGroup, request.Type));
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> Update(int id, UpdateExerciseRequest request)
        => HandleResult(await _sender.Send(new UpdateExerciseCommand(
            id, request.Name, request.Description, request.VideoUrl, request.MuscleGroup, request.Type)));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteExerciseCommand(id)));

    [HttpPost("{id:int}/video")]
    [RequestSizeLimit(UploadExerciseVideoCommandHandler.MaxVideoBytes + 1024)]
    public async Task<ActionResult<ExerciseDto>> UploadVideo(int id, IFormFile file)
        => HandleResult(await _sender.Send(new UploadExerciseVideoCommand(id, file)));

    [HttpDelete("{id:int}/video")]
    public async Task<ActionResult<ExerciseDto>> DeleteVideo(int id)
        => HandleResult(await _sender.Send(new DeleteExerciseVideoCommand(id)));
}
