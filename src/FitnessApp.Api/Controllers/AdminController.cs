using System.Text.Json;
using System.Threading.Channels;
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
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminController(ISender sender, IServiceScopeFactory scopeFactory)
    {
        _sender = sender;
        _scopeFactory = scopeFactory;
    }

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

    // Dashboard stats. Returns a one-shot JSON snapshot, or — when the caller asks for
    // text/event-stream (an EventSource) — a live SSE stream that pushes fresh stats every few
    // seconds. Both paths share the same GetAdminStatsQuery.
    [HttpGet("stats")]
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

        var channel = Channel.CreateUnbounded<AdminStatsDto>();

        // Producer: a fresh DI scope per tick keeps the DbContext short-lived.
        var producer = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    var stats = await sender.Send(new GetAdminStatsQuery(), ct);
                    await channel.Writer.WriteAsync(stats, ct);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
            catch (OperationCanceledException) { /* client disconnected */ }
            finally { channel.Writer.Complete(); }
        }, ct);

        try
        {
            await foreach (var stats in channel.Reader.ReadAllAsync(ct))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(stats, json)}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        await producer;
    }

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
