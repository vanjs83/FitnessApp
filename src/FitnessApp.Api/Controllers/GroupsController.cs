using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Application.Features.Groups.Commands;
using FitnessApp.Application.Features.Groups.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

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

    // ===== Group sessions =====

    [HttpPost("{groupId:int}/sessions")]
    public async Task<ActionResult<GroupSessionDto>> CreateSession(int groupId, CreateGroupSessionRequest request)
        => HandleCreated(await _sender.Send(new CreateGroupSessionCommand(
            groupId, request.StartsAt, request.DurationMinutes, request.Type, request.Location, request.Notes)));

    [HttpGet("sessions")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<GroupSessionDto>>> GetSessions(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        => Ok(await _sender.Send(new GetMyGroupSessionsQuery(from, to)));
}
