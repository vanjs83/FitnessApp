using Asp.Versioning;
using FitnessApp.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Controllers;

/// <summary>
/// Base controller that maps a CQRS <see cref="Result"/> onto the matching HTTP response.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleResult(Result result) =>
        result.Succeeded ? NoContent() : MapError(result);

    protected ActionResult<T> HandleResult<T>(Result<T> result) =>
        result.Succeeded ? Ok(result.Value) : MapError(result);

    /// <summary>Like HandleResult but returns 201 Created for endpoints that create a new resource.</summary>
    protected ActionResult<T> HandleCreated<T>(Result<T> result) =>
        result.Succeeded ? StatusCode(StatusCodes.Status201Created, result.Value) : MapError(result);

    protected ActionResult MapError(Result result) => result.Error switch
    {
        ResultError.NotFound => result.Message is null ? NotFound() : NotFound(new { message = result.Message }),
        ResultError.Forbidden => Forbid(),
        ResultError.Conflict => Conflict(new { message = result.Message }),
        _ => BadRequest(new { message = result.Message })
    };
}
