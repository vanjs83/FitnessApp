using FitnessApp.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

/// <summary>
/// Base controller that maps a CQRS <see cref="Result"/> onto the matching HTTP response.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleResult(Result result) =>
        result.Succeeded ? NoContent() : MapError(result);

    protected ActionResult<T> HandleResult<T>(Result<T> result) =>
        result.Succeeded ? Ok(result.Value) : MapError(result);

    protected ActionResult MapError(Result result) => result.Error switch
    {
        ResultError.NotFound => result.Message is null ? NotFound() : NotFound(new { message = result.Message }),
        ResultError.Forbidden => Forbid(),
        _ => BadRequest(new { message = result.Message })
    };
}
