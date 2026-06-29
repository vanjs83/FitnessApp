using FitnessApp.Application.DTOs.Notifications;
using FitnessApp.Application.Features.Notifications.Commands;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.Trainer)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly IMediator _sender;

    public NotificationsController(IMediator sender) => _sender = sender;

    /// <summary>Notify a client that their training/nutrition plan is ready (email + push).</summary>
    [HttpPost("notify-client-plan")]
    [ProducesResponseType<NotifyResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotifyResultDto>> NotifyClientPlan(NotifyClientPlanRequest request, CancellationToken ct)
        => HandleResult(await _sender.Send(new NotifyClientPlanCommand(
            request.ClientId, request.PlanName, request.PlanType, request.Language), ct));
}
