using HotChocolate;
using Microsoft.AspNetCore.Http;

namespace SerenAuth.Api.GraphQL;

/// <summary>
/// Error filter that prevents stack traces or internal messages from
/// reaching GraphQL clients. The original exception is kept server-side
/// by HotChocolate's logging; we surface a stable, redacted error.
/// </summary>
public sealed class SafeErrorFilter(IHttpContextAccessor accessor) : IErrorFilter
{
    public IError OnError(IError error)
    {
        var correlationId = accessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? string.Empty;

        return error
            .RemoveException()
            .WithMessage(error.Exception switch
            {
                FluentValidation.ValidationException => error.Exception.Message,
                InvalidOperationException => error.Exception.Message,
                UnauthorizedAccessException => "Unauthorized.",
                null => error.Message,
                _ => "An unexpected error occurred."
            })
            .SetExtension("correlationId", correlationId);
    }
}
