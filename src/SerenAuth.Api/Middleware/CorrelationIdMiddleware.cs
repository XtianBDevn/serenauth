using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace SerenAuth.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation ID, surfaces it on the
/// response (<c>X-Correlation-Id</c>), persists it in <c>HttpContext.Items</c>
/// for downstream use, and pushes it into Serilog's LogContext so every
/// log line for the request can be cross-referenced with the audit trail.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers.TryGetValue(Header, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        ctx.Items["CorrelationId"] = correlationId;
        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey(Header))
            {
                ctx.Response.Headers[Header] = correlationId;
            }
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(ctx);
        }
    }
}
