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
public class ProgressController : ApiControllerBase
{
    private readonly ISender _sender;

    public ProgressController(ISender sender) => _sender = sender;

    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<ProgressPhotoDto>>> GetMine([FromQuery] int? planId)
        => Ok(await _sender.Send(new GetMyProgressPhotosQuery(planId)));

    [HttpPost]
    [RequestSizeLimit(UploadProgressPhotoCommandHandler.MaxImageBytes + 1024)]
    public async Task<ActionResult<ProgressPhotoDto>> Upload(
        IFormFile file,
        [FromForm] ProgressPose pose,
        [FromForm] DateTime? takenOn,
        [FromForm] string? note,
        [FromForm] int? planId)
        => HandleCreated(await _sender.Send(new UploadProgressPhotoCommand(file, pose, takenOn, note, planId)));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => HandleResult(await _sender.Send(new DeleteProgressPhotoCommand(id)));
}
