using FluentValidation;

namespace SerenAuth.Application.Auditing;

public sealed class GetAuditEventsQueryValidator : AbstractValidator<GetAuditEventsQuery>
{
    public GetAuditEventsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 500);
        RuleFor(x => x.Action!.Value)
            .IsInEnum()
            .When(x => x.Action.HasValue);
        // A 'since' in the future would silently return nothing, so reject it
        // up front to surface the caller's mistake instead of an empty page.
        RuleFor(x => x.Since!.Value)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(1))
            .When(x => x.Since.HasValue)
            .WithMessage("'since' must not be in the future.");
    }
}
