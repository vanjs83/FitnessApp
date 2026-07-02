using System.Text.Json;
using FitnessApp.Application.Common;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Features.Admin.Commands;
using FitnessApp.Application.Features.Admin.Queries;
using FitnessApp.Application.Features.Email.Queries;
using FitnessApp.Application.Features.Messaging.Commands;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.SuperAdmin)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdminController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminController(ISender sender, IServiceScopeFactory scopeFactory)
    {
        _sender = sender;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Paged list of trainers (admin view).</summary>
    [HttpGet("trainers")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<TrainerAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TrainerAdminDto>>> GetTrainers([FromQuery] int page = 1, [FromQuery] string? search = null)
        => Ok(await _sender.Send(new GetTrainersQuery(page, search)));

    /// <summary>Create a trainer account.</summary>
    [HttpPost("trainers")]
    [ProducesResponseType<TrainerAdminDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainerAdminDto>> CreateTrainer(CreateTrainerRequest request)
        => HandleCreated(await _sender.Send(new CreateTrainerCommand(request.Email, request.FullName, request.Password)));

    /// <summary>Paged list of clients (admin view).</summary>
    [HttpGet("clients")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<ClientAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientAdminDto>>> GetClients([FromQuery] int page = 1, [FromQuery] string? search = null)
        => Ok(await _sender.Send(new GetClientsQuery(page, search)));

    /// <summary>All users available as message recipients.</summary>
    [HttpGet("recipients")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<IReadOnlyList<AdminRecipientDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminRecipientDto>>> GetRecipients()
        => Ok(await _sender.Send(new GetRecipientsQuery()));

    /// <summary>Paged list of plans (admin view).</summary>
    [HttpGet("plans")]
    [ResponseCache(CacheProfileName = "UserData")]
    [ProducesResponseType<PagedResult<PlanAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PlanAdminDto>>> GetPlans([FromQuery] int page = 1)
        => Ok(await _sender.Send(new GetPlansQuery(page)));

    // Dashboard stats. Returns a one-shot JSON snapshot, or — when the caller asks for
    // text/event-stream (an EventSource) — a live SSE stream that pushes fresh stats every few
    // seconds. Both paths share the same GetAdminStatsQuery.
    /// <summary>Dashboard stats: one-shot JSON, or a live SSE stream for an EventSource client.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task GetStats()
    {
        var ct = HttpContext.RequestAborted;
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var wantsStream = Request.Headers.Accept
            .Any(a => a is not null && a.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase));

        if (!wantsStream)
        {
            Response.ContentType = "application/json";
            var snapshot = await _sender.Send(new GetAdminStatsQuery(), ct);
            await Response.WriteAsync(JsonSerializer.Serialize(snapshot, json), ct);
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            // A fresh DI scope per tick keeps the DbContext short-lived.
            while (!ct.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var stats = await sender.Send(new GetAdminStatsQuery(), ct);

                await Response.WriteAsync($"data: {JsonSerializer.Serialize(stats, json)}\n\n", ct);
                await Response.Body.FlushAsync(ct);

                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
    }

    /// <summary>Delete a trainer account.</summary>
    [HttpDelete("trainers/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrainer(string id)
        => HandleResult(await _sender.Send(new DeleteTrainerCommand(id)));

    /// <summary>Whether SMTP email is configured.</summary>
    [HttpGet("email/status")]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<EmailStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailStatusDto>> GetEmailStatus()
        => Ok(await _sender.Send(new GetEmailStatusQuery()));

    /// <summary>Email a set of trainers.</summary>
    [HttpPost("email/send-to-trainers")]
    [ProducesResponseType<EmailSendResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailSendResultDto>> SendToTrainers(SendEmailToTrainersRequest request)
        => HandleResult(await _sender.Send(new SendEmailToTrainersCommand(request.TrainerIds, request.Subject, request.Body, request.Language)));

    /// <summary>Queue an email to a set of users (delivered now, or at SendAtUtc if given).</summary>
    [HttpPost("email/send-to-users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmailToUsers(SendMessageToUsersRequest request)
        => HandleResult(await _sender.Send(new SendEmailToUsersCommand(request.UserIds, request.Subject, request.Body, request.SendAtUtc)));

    /// <summary>Queue a push notification to a set of users (delivered now, or at SendAtUtc if given).</summary>
    [HttpPost("push/send")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendPushToUsers(SendMessageToUsersRequest request)
        => HandleResult(await _sender.Send(new SendPushToUsersCommand(request.UserIds, request.Subject, request.Body, request.SendAtUtc)));
}
