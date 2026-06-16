using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Features.Admin.Commands;
using FitnessApp.Application.Features.Admin.Queries;
using FitnessApp.Application.Features.Email.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.SuperAdmin)]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdminController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender) => _sender = sender;

    [HttpGet("trainers")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<PagedResult<TrainerAdminDto>>> GetTrainers([FromQuery] int page = 1, [FromQuery] string? search = null)
        => Ok(await _sender.Send(new GetTrainersQuery(page, search)));

    [HttpPost("trainers")]
    public async Task<ActionResult<TrainerAdminDto>> CreateTrainer(CreateTrainerRequest request)
        => HandleResult(await _sender.Send(new CreateTrainerCommand(request.Email, request.FullName, request.Password)));

    [HttpGet("clients")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<PagedResult<ClientAdminDto>>> GetClients([FromQuery] int page = 1, [FromQuery] string? search = null)
        => Ok(await _sender.Send(new GetClientsQuery(page, search)));

    [HttpGet("recipients")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<IReadOnlyList<AdminRecipientDto>>> GetRecipients()
        => Ok(await _sender.Send(new GetRecipientsQuery()));

    [HttpGet("plans")]
    [ResponseCache(CacheProfileName = "UserData")]
    public async Task<ActionResult<PagedResult<PlanAdminDto>>> GetPlans([FromQuery] int page = 1)
        => Ok(await _sender.Send(new GetPlansQuery(page)));

    [HttpGet("stats")]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<AdminStatsDto>> GetStats()
        => Ok(await _sender.Send(new GetAdminStatsQuery()));

    [HttpDelete("trainers/{id}")]
    public async Task<IActionResult> DeleteTrainer(string id)
        => HandleResult(await _sender.Send(new DeleteTrainerCommand(id)));

    [HttpGet("email/status")]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<EmailStatusDto>> GetEmailStatus()
        => Ok(await _sender.Send(new GetEmailStatusQuery()));

    [HttpPost("email/send-to-trainers")]
    public async Task<ActionResult<EmailSendResultDto>> SendToTrainers(SendEmailToTrainersRequest request)
        => HandleResult(await _sender.Send(new SendEmailToTrainersCommand(request.TrainerIds, request.Subject, request.Body, request.Language)));

    [HttpPost("email/send-to-users")]
    public async Task<ActionResult<MessageSendResultDto>> SendEmailToUsers(SendMessageToUsersRequest request)
        => HandleResult(await _sender.Send(new SendEmailToUsersCommand(request.UserIds, request.Subject, request.Body)));

    [HttpPost("push/send")]
    public async Task<ActionResult<MessageSendResultDto>> SendPushToUsers(SendMessageToUsersRequest request)
        => HandleResult(await _sender.Send(new SendPushToUsersCommand(request.UserIds, request.Subject, request.Body)));
}
