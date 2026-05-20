using MediatR;
using SerenAuth.Application.Dtos;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.PriorAuthorizations;

/// <summary>
/// Creates a draft prior authorization. The organizationId on the
/// resulting entity is taken from the caller's JWT, never the client.
/// </summary>
public sealed record CreatePriorAuthorizationCommand(
    string PatientId,
    string ProviderId,
    string ProcedureCpt,
    string DiagnosisIcd10,
    string Payer,
    double AiConfidence) : IRequest<PriorAuthorizationDto>;

/// <summary>Submits a draft, transitioning Draft → Pending.</summary>
public sealed record SubmitPriorAuthorizationCommand(string Id) : IRequest<PriorAuthorizationDto>;

/// <summary>
/// Lists PAs for the caller's organization. Returned data is filtered
/// server-side so a client cannot read another tenant's PAs.
/// </summary>
public sealed record GetPriorAuthorizationsQuery(
    PaStatus? Status,
    string? Payer,
    int Limit = 50) : IRequest<IReadOnlyList<PriorAuthorizationDto>>;

public sealed record GetPatientsQuery(int Limit = 100) : IRequest<IReadOnlyList<PatientDto>>;

public sealed record GetProvidersQuery(int Limit = 100) : IRequest<IReadOnlyList<ProviderDto>>;
