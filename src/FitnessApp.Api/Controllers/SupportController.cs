using FitnessApp.Application.DTOs.Support;
using FitnessApp.Application.Features.Support.Commands;
using FitnessApp.Application.Features.Support.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/support")]
public class SupportController : ApiControllerBase
{
    private readonly ISender _sender;

    public SupportController(ISender sender) => _sender = sender;

    /// <summary>Whether the support contact channel is configured/available.</summary>
    [HttpGet("status")]
    [ResponseCache(CacheProfileName = "Volatile")]
    [ProducesResponseType<SupportStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportStatusDto>> GetStatus()
        => Ok(await _sender.Send(new GetSupportStatusQuery()));

    /// <summary>Send a support/contact message.</summary>
    [HttpPost("contact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Contact(SupportContactRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ContactSupportCommand(request.Subject, request.Body, request.Language), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { sent = true });
    }
}
