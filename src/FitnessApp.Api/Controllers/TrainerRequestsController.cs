using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.Features.TrainerRequests.Commands;
using FitnessApp.Application.Features.TrainerRequests.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/trainer-requests")]
public class TrainerRequestsController : ApiControllerBase
{
    private readonly ISender _sender;

    public TrainerRequestsController(ISender sender) => _sender = sender;

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

    // ===== Client side =====

    [HttpPost]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<MyTrainerRequestDto>> SendRequest(CreateTrainerRequestRequest request)
        => HandleCreated(await _sender.Send(new SendTrainerRequestCommand(request.TrainerId, request.Language, PublicBaseUrl())));

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Client)]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<MyTrainerRequestDto?>> GetMine()
        => Ok(await _sender.Send(new GetMyTrainerRequestQuery()));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> Cancel(int id)
        => HandleResult(await _sender.Send(new CancelTrainerRequestCommand(id)));

    // ===== Trainer side =====

    [HttpGet("incoming")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<IReadOnlyList<IncomingTrainerRequestDto>>> GetIncoming()
        => Ok(await _sender.Send(new GetIncomingTrainerRequestsQuery()));

    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Accept(int id)
    {
        var result = await _sender.Send(new AcceptTrainerRequestCommand(id));
        return result.Succeeded ? Ok(new { clientId = result.Value }) : MapError(result);
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Reject(int id)
        => HandleResult(await _sender.Send(new RejectTrainerRequestCommand(id)));
}
