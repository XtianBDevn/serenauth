using Microsoft.AspNetCore.Http;

namespace SerenAuth.Api.Middleware;

/// <summary>
/// Applies a baseline set of HTTP security headers on every response.
/// Mirrors the Helmet defaults used in Node services so the platform's
/// security posture is consistent across surfaces.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext ctx)
    {
        ctx.Response.OnStarting(() =>
        {
            var h = ctx.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=()";
            h["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
            return Task.CompletedTask;
        });
        return next(ctx);
    }
}
