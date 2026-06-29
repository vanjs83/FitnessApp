using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Application.Features.Groups.Commands;
using FitnessApp.Application.Features.Groups.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

// Group management only. Booking/cancelling a group session goes through AppointmentsController
// (an appointment with GroupId), and group sessions show up in the normal /appointments feed.
[Authorize(Roles = Roles.Trainer)]
[Route("api/v{version:apiVersion}/groups")]
public class GroupsController : ApiControllerBase
{
    private readonly ISender _sender;

    public GroupsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<TrainingGroupDto>>> GetMine()
        => Ok(await _sender.Send(new GetMyGroupsQuery()));

    [HttpPost]
    public async Task<ActionResult<TrainingGroupDto>> Create(CreateGroupRequest request)
        => HandleCreated(await _sender.Send(new CreateGroupCommand(request.Name, request.ClientIds)));

    [HttpPost("{groupId:int}/members")]
    public async Task<ActionResult<TrainingGroupDto>> AddMember(int groupId, AddGroupMemberRequest request)
        => HandleResult(await _sender.Send(new AddGroupMemberCommand(groupId, request.ClientId)));

    [HttpDelete("{groupId:int}/members/{clientId}")]
    public async Task<IActionResult> RemoveMember(int groupId, string clientId)
        => HandleResult(await _sender.Send(new RemoveGroupMemberCommand(groupId, clientId)));

    [HttpDelete("{groupId:int}")]
    public async Task<IActionResult> Delete(int groupId)
        => HandleResult(await _sender.Send(new DeleteGroupCommand(groupId)));

    // Broadcast an email and/or push to every member of the group.
    [HttpPost("{groupId:int}/message")]
    public async Task<ActionResult<GroupMessageResultDto>> SendMessage(int groupId, SendMessageToGroupRequest request)
        => HandleResult(await _sender.Send(new SendMessageToGroupCommand(
            groupId, request.Subject, request.Body, request.Email, request.Push)));
}
