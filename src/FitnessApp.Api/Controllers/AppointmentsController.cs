using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Appointments.Commands;
using FitnessApp.Application.Features.Appointments.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class AppointmentsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AppointmentsController(ISender sender) => _sender = sender;

    // ===== Shared (trainer or client) =====

    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMine(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        => Ok(await _sender.Send(new GetMyAppointmentsQuery(from, to)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentDto>> GetById(int id)
        => HandleResult(await _sender.Send(new GetAppointmentByIdQuery(id)));

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
        => HandleResult(await _sender.Send(new CancelAppointmentCommand(id)));

    // ===== Trainer =====

    [HttpPost]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request)
        => HandleCreated(await _sender.Send(new CreateAppointmentCommand(
            request.ClientId, request.GroupId, request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<ActionResult<AppointmentDto>> Update(int id, UpdateAppointmentRequest request)
        => HandleResult(await _sender.Send(new UpdateAppointmentCommand(
            id, request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Confirm(int id)
        => HandleResult(await _sender.Send(new ConfirmAppointmentCommand(id)));

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> Complete(int id)
        => HandleResult(await _sender.Send(new CompleteAppointmentCommand(id)));

    [HttpPost("{id:int}/no-show")]
    [Authorize(Roles = Roles.Trainer)]
    public async Task<IActionResult> NoShow(int id)
        => HandleResult(await _sender.Send(new MarkNoShowCommand(id)));

    // ===== Client =====

    [HttpPost("request")]
    [Authorize(Roles = Roles.Client)]
    public new async Task<ActionResult<AppointmentDto>> Request(RequestAppointmentRequest request)
        => HandleCreated(await _sender.Send(new RequestAppointmentCommand(
            request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    // A group member confirms / withdraws attendance for a group session.
    [HttpPost("{id:int}/attend")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> Attend(int id)
        => HandleResult(await _sender.Send(new ConfirmGroupAttendanceCommand(id)));

    [HttpDelete("{id:int}/attend")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> Unattend(int id)
        => HandleResult(await _sender.Send(new CancelGroupAttendanceCommand(id)));
}
