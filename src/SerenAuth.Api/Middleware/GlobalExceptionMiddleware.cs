using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SerenAuth.Api.Middleware;

/// <summary>
/// Translates unhandled exceptions into safe RFC 7807 ProblemDetails
/// responses. Stack traces are never returned to the client; full
/// details are logged server-side with the request's correlation ID.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (ValidationException vex)
        {
            log.LogWarning(vex, "Validation failed.");
            await WriteProblem(ctx, StatusCodes.Status400BadRequest, "Validation failed", vex.Errors.Select(e => e.ErrorMessage));
        }
        catch (UnauthorizedAccessException uex)
        {
            log.LogWarning(uex, "Unauthorized access.");
            await WriteProblem(ctx, StatusCodes.Status401Unauthorized, "Unauthorized");
        }
        catch (InvalidOperationException iex)
        {
            log.LogWarning(iex, "Invalid operation.");
            await WriteProblem(ctx, StatusCodes.Status409Conflict, iex.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled exception.");
            await WriteProblem(ctx, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static Task WriteProblem(HttpContext ctx, int status, string title, IEnumerable<string>? errors = null)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://serenauth.dev/errors/{status}",
            title,
            status,
            correlationId = ctx.Items["CorrelationId"]?.ToString(),
            errors = errors?.ToArray()
        };
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
