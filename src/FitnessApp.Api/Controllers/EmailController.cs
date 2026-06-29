using FitnessApp.Application.DTOs.Email;
using FitnessApp.Application.Features.Email.Commands;
using FitnessApp.Application.Features.Email.Queries;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize(Roles = Roles.Trainer)]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/email")]
public class EmailController : ApiControllerBase
{
    private readonly ISender _sender;

    public EmailController(ISender sender) => _sender = sender;

    /// <summary>Whether SMTP email is configured.</summary>
    [HttpGet("status")]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<EmailStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailStatusDto>> GetStatus()
        => Ok(await _sender.Send(new GetEmailStatusQuery()));

    /// <summary>Email a client that their plan is ready.</summary>
    [HttpPost("notify-plan-ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyPlanReady([FromBody] NotifyPlanReadyRequest request, CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/";
        var result = await _sender.Send(new NotifyPlanReadyCommand(
            request.ClientId, request.PlanName, request.PlanType, request.Language, baseUrl), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { sent = true, to = result.Value!.To });
    }

    /// <summary>Send a free-form email to one of the trainer's clients.</summary>
    [HttpPost("send-to-client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToClient(SendEmailToClientRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SendEmailToClientCommand(
            request.ClientId, request.Subject, request.Body, request.Language), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { sent = true, to = result.Value!.To, from = result.Value.From });
    }
}
