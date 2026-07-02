using FitnessApp.Application.DTOs.Messaging;
using FitnessApp.Application.Features.ScheduledMessages.Commands;
using FitnessApp.Application.Features.ScheduledMessages.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

/// <summary>Trainers and admins schedule push/email messages to be delivered at a future time.</summary>
[Authorize(Roles = $"{Roles.Trainer},{Roles.SuperAdmin}")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ScheduledMessagesController(ISender sender) : ApiControllerBase
{
    private readonly ISender _sender = sender;

    /// <summary>The caller's own scheduled messages (newest send time first).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ScheduledMessageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScheduledMessageDto>>> GetMine()
        => Ok(await _sender.Send(new GetMyScheduledMessagesQuery()));

    /// <summary>Schedule a push or email for a client or a whole group at a future time.</summary>
    [HttpPost]
    [ProducesResponseType<ScheduledMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledMessageDto>> Schedule(ScheduleMessageRequest request)
        => HandleCreated(await _sender.Send(new ScheduleMessageCommand(
            request.Channel, request.Audience, request.UserId, request.GroupId,
            request.Subject, request.Body, request.SendAtUtc)));

    /// <summary>Cancel a still-pending scheduled message.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
        => HandleResult(await _sender.Send(new CancelScheduledMessageCommand(id)));
}
