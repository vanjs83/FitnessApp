using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Middleware;

/// <summary>
/// Central place where failed inserts/updates (and any other unhandled exception) are turned
/// into a clean HTTP response instead of a raw 500. Covers every controller and CQRS handler,
/// so individual save operations don't need their own try/catch.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning(exception, "Request failed on {Method} {Path}: {Title}",
                httpContext.Request.Method, httpContext.Request.Path, title);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title
        };
        if (_env.IsDevelopment())
            problem.Detail = exception.Message;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title) Map(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException =>
            (StatusCodes.Status409Conflict, "The record was modified by another operation. Please retry."),

        DbUpdateException dbEx when dbEx.GetBaseException() is SqlException sql => sql.Number switch
        {
            2627 or 2601 => (StatusCodes.Status409Conflict, "A record with the same value already exists."),
            547 => (StatusCodes.Status400BadRequest, "The operation violates a data relationship constraint."),
            _ => (StatusCodes.Status500InternalServerError, "A database error occurred while saving changes.")
        },

        DbUpdateException =>
            (StatusCodes.Status500InternalServerError, "A database error occurred while saving changes."),

        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
