using FluentValidation;
using SerenAuth.Domain.ValueObjects;

namespace SerenAuth.Application.PriorAuthorizations;

public sealed class CreatePriorAuthorizationCommandValidator
    : AbstractValidator<CreatePriorAuthorizationCommand>
{
    public CreatePriorAuthorizationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ProviderId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ProcedureCpt)
            .NotEmpty()
            .Must(CptCode.Allowed.Contains)
            .WithMessage("CPT code is not in the dialysis MVP allowlist.");
        RuleFor(x => x.DiagnosisIcd10)
            .NotEmpty()
            .Must(Icd10Code.Allowed.Contains)
            .WithMessage("ICD-10 code is not in the dialysis MVP allowlist.");
        RuleFor(x => x.Payer).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AiConfidence).InclusiveBetween(0.0, 1.0);
    }
}

public sealed class SubmitPriorAuthorizationCommandValidator
    : AbstractValidator<SubmitPriorAuthorizationCommand>
{
    public SubmitPriorAuthorizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(64);
    }
}

public sealed class UpdatePriorAuthorizationCommandValidator
    : AbstractValidator<UpdatePriorAuthorizationCommand>
{
    public UpdatePriorAuthorizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ProcedureCpt)
            .NotEmpty()
            .Must(CptCode.Allowed.Contains)
            .WithMessage("CPT code is not in the dialysis MVP allowlist.");
        RuleFor(x => x.DiagnosisIcd10)
            .NotEmpty()
            .Must(Icd10Code.Allowed.Contains)
            .WithMessage("ICD-10 code is not in the dialysis MVP allowlist.");
        RuleFor(x => x.Payer).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AiConfidence).InclusiveBetween(0.0, 1.0);
    }
}

public sealed class DecidePriorAuthorizationCommandValidator
    : AbstractValidator<DecidePriorAuthorizationCommand>
{
    public DecidePriorAuthorizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Decision).IsInEnum();
    }
}

public sealed class GetPriorAuthorizationsQueryValidator
    : AbstractValidator<GetPriorAuthorizationsQuery>
{
    public GetPriorAuthorizationsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}
