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

/// <summary>
/// Edits the clinical fields of a draft PA. Refuses to touch a PA that
/// has already been submitted — that guarantee lives on the entity, the
/// handler just translates the failure into a transport-level error.
/// </summary>
public sealed record UpdatePriorAuthorizationCommand(
    string Id,
    string ProcedureCpt,
    string DiagnosisIcd10,
    string Payer,
    double AiConfidence) : IRequest<PriorAuthorizationDto>;

/// <summary>Submits a draft, transitioning Draft → Pending.</summary>
public sealed record SubmitPriorAuthorizationCommand(string Id) : IRequest<PriorAuthorizationDto>;

/// <summary>
/// Records the payer's decision on a Pending PA. Admin-only — the
/// decision closes the lifecycle so it must be deliberate. Approve and
/// Deny are modeled as a single command + enum so they share the audit
/// path and the policy gate.
/// </summary>
public enum PaDecision
{
    Approve = 0,
    Deny = 1
}

public sealed record DecidePriorAuthorizationCommand(string Id, PaDecision Decision)
    : IRequest<PriorAuthorizationDto>;

/// <summary>
/// Withdraws a Pending PA — the clinic taking back its own submission
/// before the payer responds. Sibling of submit (same policy) so the
/// roles that can push it out are also the ones that can pull it back.
/// </summary>
public sealed record WithdrawPriorAuthorizationCommand(string Id)
    : IRequest<PriorAuthorizationDto>;

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
