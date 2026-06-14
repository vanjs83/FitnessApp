using FitnessApp.Application.DTOs.Support;
using FitnessApp.Application.Features.Support.Commands;
using FitnessApp.Application.Features.Support.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/support")]
public class SupportController : ApiControllerBase
{
    private readonly ISender _sender;

    public SupportController(ISender sender) => _sender = sender;

    [HttpGet("status")]
    [ResponseCache(CacheProfileName = "Volatile")]
    public async Task<ActionResult<SupportStatusDto>> GetStatus()
        => Ok(await _sender.Send(new GetSupportStatusQuery()));

    [HttpPost("contact")]
    public async Task<IActionResult> Contact(SupportContactRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ContactSupportCommand(request.Subject, request.Body, request.Language), ct);
        if (!result.Succeeded) return MapError(result);
        return Ok(new { sent = true });
    }
}
