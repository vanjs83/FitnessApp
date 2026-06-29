using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.Features.TrainerRequests.Commands;
using FitnessApp.Application.Features.TrainerRequests.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/trainer-requests")]
public class TrainerRequestsController : ApiControllerBase
{
    private readonly ISender _sender;

    public TrainerRequestsController(ISender sender) => _sender = sender;

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host}/";

    // ===== Client side =====

    /// <summary>Client sends a request to be taken on by a trainer.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<MyTrainerRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MyTrainerRequestDto>> SendRequest(CreateTrainerRequestRequest request)
        => HandleCreated(await _sender.Send(new SendTrainerRequestCommand(request.TrainerId, request.Language, PublicBaseUrl())));

    /// <summary>The client's current outgoing request, if any.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Client)]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<MyTrainerRequestDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyTrainerRequestDto?>> GetMine()
        => Ok(await _sender.Send(new GetMyTrainerRequestQuery()));

    /// <summary>Client cancels their pending request.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
        => HandleResult(await _sender.Send(new CancelTrainerRequestCommand(id)));

    // ===== Trainer side =====

    /// <summary>Requests awaiting the trainer's decision.</summary>
    [HttpGet("incoming")]
    [Authorize(Roles = Roles.Trainer)]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<IReadOnlyList<IncomingTrainerRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IncomingTrainerRequestDto>>> GetIncoming()
        => Ok(await _sender.Send(new GetIncomingTrainerRequestsQuery()));

    /// <summary>Trainer accepts a request, linking the client.</summary>
    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Accept(int id)
    {
        var result = await _sender.Send(new AcceptTrainerRequestCommand(id));
        return result.Succeeded ? Ok(new { clientId = result.Value }) : MapError(result);
    }

    /// <summary>Trainer rejects a request.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id)
        => HandleResult(await _sender.Send(new RejectTrainerRequestCommand(id)));
}
