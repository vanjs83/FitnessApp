using FitnessApp.Application.DTOs.Notifications;
using FitnessApp.Application.Features.Notifications.Commands;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.Trainer)]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly IMediator _sender;

    public NotificationsController(IMediator sender) => _sender = sender;

    [HttpPost("notify-client-plan")]
    public async Task<ActionResult<NotifyResultDto>> NotifyClientPlan(NotifyClientPlanRequest request, CancellationToken ct)
        => HandleResult(await _sender.Send(new NotifyClientPlanCommand(
            request.ClientId, request.PlanName, request.PlanType, request.Language), ct));
}
