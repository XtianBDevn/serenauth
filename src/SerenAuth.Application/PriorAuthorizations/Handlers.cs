using MediatR;
using SerenAuth.Application.Abstractions;
using SerenAuth.Application.Dtos;
using SerenAuth.Domain.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;
using SerenAuth.Domain.ValueObjects;

namespace SerenAuth.Application.PriorAuthorizations;

public sealed class CreatePriorAuthorizationHandler(
    IPriorAuthorizationRepository repo,
    IPatientRepository patients,
    IProviderRepository providers,
    ICurrentUser currentUser,
    IAuditPublisher audit)
    : IRequestHandler<CreatePriorAuthorizationCommand, PriorAuthorizationDto>
{
    public async Task<PriorAuthorizationDto> Handle(
        CreatePriorAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        // Enforce tenant isolation: patient + provider must belong to the caller's org.
        var orgId = currentUser.OrganizationId;
        var patient = await patients.GetAsync(orgId, request.PatientId, cancellationToken)
            ?? throw new InvalidOperationException("Patient not found in organization.");
        var provider = await providers.GetAsync(orgId, request.ProviderId, cancellationToken)
            ?? throw new InvalidOperationException("Provider not found in organization.");

        var pa = PriorAuthorization.CreateDraft(
            orgId,
            patient.Id,
            provider.Id,
            CptCode.Create(request.ProcedureCpt),
            Icd10Code.Create(request.DiagnosisIcd10),
            Payer.Create(request.Payer),
            request.AiConfidence);

        await repo.InsertAsync(pa, cancellationToken);
        await audit.PublishAsync(AuditAction.CREATE_PA, nameof(PriorAuthorization), pa.Id, cancellationToken);

        return PriorAuthorizationDto.FromEntity(pa);
    }
}

public sealed class UpdatePriorAuthorizationHandler(
    IPriorAuthorizationRepository repo,
    ICurrentUser currentUser,
    IAuditPublisher audit)
    : IRequestHandler<UpdatePriorAuthorizationCommand, PriorAuthorizationDto>
{
    public async Task<PriorAuthorizationDto> Handle(
        UpdatePriorAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var pa = await repo.GetAsync(currentUser.OrganizationId, request.Id, cancellationToken)
            ?? throw new InvalidOperationException("PriorAuthorization not found.");

        // Value-object construction enforces the same allowlist + range
        // guarantees as CreateDraft. The domain Update method enforces
        // the Draft-only invariant.
        pa.Update(
            CptCode.Create(request.ProcedureCpt),
            Icd10Code.Create(request.DiagnosisIcd10),
            Payer.Create(request.Payer),
            request.AiConfidence);

        await repo.UpdateAsync(pa, cancellationToken);
        await audit.PublishAsync(AuditAction.UPDATE_PA, nameof(PriorAuthorization), pa.Id, cancellationToken);

        return PriorAuthorizationDto.FromEntity(pa);
    }
}

public sealed class SubmitPriorAuthorizationHandler(
    IPriorAuthorizationRepository repo,
    ICurrentUser currentUser,
    IAuditPublisher audit)
    : IRequestHandler<SubmitPriorAuthorizationCommand, PriorAuthorizationDto>
{
    public async Task<PriorAuthorizationDto> Handle(
        SubmitPriorAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var pa = await repo.GetAsync(currentUser.OrganizationId, request.Id, cancellationToken)
            ?? throw new InvalidOperationException("PriorAuthorization not found.");

        pa.Submit();

        await repo.UpdateAsync(pa, cancellationToken);
        await audit.PublishAsync(AuditAction.SUBMIT_PA, nameof(PriorAuthorization), pa.Id, cancellationToken);

        return PriorAuthorizationDto.FromEntity(pa);
    }
}

public sealed class DecidePriorAuthorizationHandler(
    IPriorAuthorizationRepository repo,
    ICurrentUser currentUser,
    IAuditPublisher audit)
    : IRequestHandler<DecidePriorAuthorizationCommand, PriorAuthorizationDto>
{
    public async Task<PriorAuthorizationDto> Handle(
        DecidePriorAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var pa = await repo.GetAsync(currentUser.OrganizationId, request.Id, cancellationToken)
            ?? throw new InvalidOperationException("PriorAuthorization not found.");

        switch (request.Decision)
        {
            case PaDecision.Approve:
                pa.Approve();
                break;
            case PaDecision.Deny:
                pa.Deny();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Decision, "Unknown PA decision.");
        }

        await repo.UpdateAsync(pa, cancellationToken);
        await audit.PublishAsync(AuditAction.DECIDE_PA, nameof(PriorAuthorization), pa.Id, cancellationToken);

        return PriorAuthorizationDto.FromEntity(pa);
    }
}

public sealed class GetPriorAuthorizationsHandler(
    IPriorAuthorizationRepository repo,
    ICurrentUser currentUser,
    IAuditPublisher audit)
    : IRequestHandler<GetPriorAuthorizationsQuery, IReadOnlyList<PriorAuthorizationDto>>
{
    public async Task<IReadOnlyList<PriorAuthorizationDto>> Handle(
        GetPriorAuthorizationsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await repo.ListAsync(
            currentUser.OrganizationId,
            request.Status,
            request.Payer,
            request.Limit,
            cancellationToken);

        // VIEW_PA at the aggregate level — we log one event per list call to
        // avoid PHI-by-row in the audit trail.
        await audit.PublishAsync(AuditAction.VIEW_PA, nameof(PriorAuthorization), "list", cancellationToken);

        return items.Select(PriorAuthorizationDto.FromEntity).ToList();
    }
}

public sealed class GetPatientsHandler(
    IPatientRepository patients,
    ICurrentUser currentUser)
    : IRequestHandler<GetPatientsQuery, IReadOnlyList<PatientDto>>
{
    public async Task<IReadOnlyList<PatientDto>> Handle(
        GetPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await patients.ListByOrganizationAsync(currentUser.OrganizationId, cancellationToken);
        return items.Take(request.Limit).Select(PatientDto.FromEntity).ToList();
    }
}

public sealed class GetProvidersHandler(
    IProviderRepository providers,
    ICurrentUser currentUser)
    : IRequestHandler<GetProvidersQuery, IReadOnlyList<ProviderDto>>
{
    public async Task<IReadOnlyList<ProviderDto>> Handle(
        GetProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var items = await providers.ListByOrganizationAsync(currentUser.OrganizationId, cancellationToken);
        return items.Take(request.Limit).Select(ProviderDto.FromEntity).ToList();
    }
}
