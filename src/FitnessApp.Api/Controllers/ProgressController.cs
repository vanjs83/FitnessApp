using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.Features.Progress.Commands;
using FitnessApp.Application.Features.Progress.Queries;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = Roles.Client)]
[Produces("application/json")]
public class ProgressController : ApiControllerBase
{
    private readonly ISender _sender;

    public ProgressController(ISender sender) => _sender = sender;

    /// <summary>The client's progress photos, optionally filtered by plan.</summary>
    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<ProgressPhotoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProgressPhotoDto>>> GetMine([FromQuery] int? planId)
        => Ok(await _sender.Send(new GetMyProgressPhotosQuery(planId)));

    /// <summary>Upload a progress photo.</summary>
    [HttpPost]
    [RequestSizeLimit(UploadProgressPhotoCommandHandler.MaxImageBytes + 1024)]
    [ProducesResponseType<ProgressPhotoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProgressPhotoDto>> Upload(
        IFormFile file,
        [FromForm] ProgressPose pose,
        [FromForm] DateTime? takenOn,
        [FromForm] string? note,
        [FromForm] int? planId)
        => HandleCreated(await _sender.Send(new UploadProgressPhotoCommand(file, pose, takenOn, note, planId)));

    /// <summary>Delete a progress photo.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteProgressPhotoCommand(id)));
}
