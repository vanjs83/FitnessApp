using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Application.Features.Groups.Commands;
using FitnessApp.Application.Features.Groups.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

// Group management only. Booking/cancelling a group session goes through AppointmentsController
// (an appointment with GroupId), and group sessions show up in the normal /appointments feed.
[Authorize(Roles = Roles.Trainer)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/groups")]
public class GroupsController : ApiControllerBase
{
    private readonly ISender _sender;

    public GroupsController(ISender sender) => _sender = sender;

    /// <summary>The current trainer's active groups, each with its members.</summary>
    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<TrainingGroupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrainingGroupDto>>> GetMine()
        => Ok(await _sender.Send(new GetMyGroupsQuery()));

    /// <summary>Create a group from a (possibly empty) subset of the trainer's own clients.</summary>
    [HttpPost]
    [ProducesResponseType<TrainingGroupDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrainingGroupDto>> Create(CreateGroupRequest request)
        => HandleCreated(await _sender.Send(new CreateGroupCommand(request.Name, request.ClientIds)));

    /// <summary>Add one of the trainer's clients to a group.</summary>
    [HttpPost("{groupId:int}/members")]
    [ProducesResponseType<TrainingGroupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingGroupDto>> AddMember(int groupId, AddGroupMemberRequest request)
        => HandleResult(await _sender.Send(new AddGroupMemberCommand(groupId, request.ClientId)));

    /// <summary>Remove a member from a group.</summary>
    [HttpDelete("{groupId:int}/members/{clientId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(int groupId, string clientId)
        => HandleResult(await _sender.Send(new RemoveGroupMemberCommand(groupId, clientId)));

    /// <summary>Delete a group.</summary>
    [HttpDelete("{groupId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int groupId)
        => HandleResult(await _sender.Send(new DeleteGroupCommand(groupId)));

    /// <summary>Broadcast an email and/or push to every member of the group.</summary>
    [HttpPost("{groupId:int}/message")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(int groupId, SendMessageToGroupRequest request)
        => HandleResult(await _sender.Send(new SendMessageToGroupCommand(
            groupId, request.Subject, request.Body, request.Email, request.Push)));
}
