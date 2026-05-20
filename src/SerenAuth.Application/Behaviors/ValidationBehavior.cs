using FluentValidation;
using MediatR;

namespace SerenAuth.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs every registered FluentValidation
/// validator before the handler executes. Failures aggregate into a
/// single <see cref="ValidationException"/> so callers see all issues at
/// once.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var v in validators)
        {
            var result = await v.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
