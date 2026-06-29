using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Appointments.Commands;
using FitnessApp.Application.Features.Appointments.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AppointmentsController(ISender sender) : ApiControllerBase
{
    private readonly ISender _sender = sender;

    // ===== Shared (trainer or client) =====

    /// <summary>The caller's appointments (as trainer or client) within an optional date window.</summary>
    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<AppointmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMine(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        => Ok(await _sender.Send(new GetMyAppointmentsQuery(from, to)));

    /// <summary>A single appointment the caller takes part in.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetAppointmentByIdQuery(id)));

    /// <summary>Cancel an open appointment (removes it).</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
        => HandleResult(await _sender.Send(new CancelAppointmentCommand(id)));

    // ===== Trainer =====

    /// <summary>Trainer books an individual (ClientId) or group (GroupId) session.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request)
        => HandleCreated(await _sender.Send(new CreateAppointmentCommand(
            request.ClientId, request.GroupId, request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    /// <summary>Trainer reschedules / edits a session.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Update(int id, UpdateAppointmentRequest request)
        => HandleResult(await _sender.Send(new UpdateAppointmentCommand(
            id, request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    /// <summary>Trainer confirms a client-requested slot.</summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(int id)
        => HandleResult(await _sender.Send(new ConfirmAppointmentCommand(id)));

    /// <summary>Trainer marks a scheduled session as completed.</summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(int id)
        => HandleResult(await _sender.Send(new CompleteAppointmentCommand(id)));

    /// <summary>Trainer marks a scheduled session as a no-show.</summary>
    [HttpPost("{id:int}/no-show")]
    [Authorize(Roles = Roles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NoShow(int id)
        => HandleResult(await _sender.Send(new MarkNoShowCommand(id)));

    // ===== Client =====

    /// <summary>Client proposes a slot to their trainer (awaits confirmation).</summary>
    [HttpPost("request")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public new async Task<ActionResult<AppointmentDto>> Request(RequestAppointmentRequest request)
        => HandleCreated(await _sender.Send(new RequestAppointmentCommand(
            request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    /// <summary>A group member confirms they will attend the group session.</summary>
    [HttpPost("{id:int}/attend")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Attend(int id)
        => HandleResult(await _sender.Send(new ConfirmGroupAttendanceCommand(id)));

    /// <summary>A group member withdraws their attendance confirmation.</summary>
    [HttpDelete("{id:int}/attend")]
    [Authorize(Roles = Roles.Client)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unattend(int id)
        => HandleResult(await _sender.Send(new CancelGroupAttendanceCommand(id)));
}
