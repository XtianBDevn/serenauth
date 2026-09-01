using FluentValidation;

namespace SerenAuth.Application.Auth;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(256);

        // Hard floor of 12 chars on the new password — well above the NIST 800-63B
        // minimum of 8 and aligned with what reviewers expect for a HIPAA-conscious
        // posture. The full crack-cost story lives in PBKDF2 (100k iterations).
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(256);

        // Reject no-op rotations. Saves an audit row and avoids the very
        // confusing UX where "I changed my password" succeeds without
        // actually changing anything.
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from current password.");
    }
}
