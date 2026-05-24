using HotChocolate;
using HotChocolate.Authorization;
using MediatR;
using SerenAuth.Api.Authorization;
using SerenAuth.Application.Auditing;
using SerenAuth.Application.Dtos;
using SerenAuth.Application.PriorAuthorizations;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Api.GraphQL;

/// <summary>
/// GraphQL root Query. Every resolver is authorized + scoped to the
/// caller's organization in the underlying handler.
/// </summary>
public sealed class Query
{
    [Authorize(Policy = Policies.RequirePaRead)]
    public Task<IReadOnlyList<PriorAuthorizationDto>> PriorAuthorizations(
        [Service] IMediator mediator,
        PaStatus? status,
        string? payer,
        int limit = 50,
        CancellationToken ct = default)
        => mediator.Send(new GetPriorAuthorizationsQuery(status, payer, limit), ct);

    [Authorize(Policy = Policies.RequireOrgScope)]
    public Task<IReadOnlyList<PatientDto>> Patients(
        [Service] IMediator mediator,
        int limit = 100,
        CancellationToken ct = default)
        => mediator.Send(new GetPatientsQuery(limit), ct);

    [Authorize(Policy = Policies.RequireOrgScope)]
    public Task<IReadOnlyList<ProviderDto>> Providers(
        [Service] IMediator mediator,
        int limit = 100,
        CancellationToken ct = default)
        => mediator.Send(new GetProvidersQuery(limit), ct);

    /// <summary>
    /// Admin-only read of the append-only audit log, scoped to the
    /// caller's organization. Mirrors the privilege model of
    /// decidePriorAuthorization: most powerful read is gated by the
    /// most restrictive policy.
    /// </summary>
    [Authorize(Policy = Policies.RequireAdmin)]
    public Task<IReadOnlyList<AuditEventDto>> AuditEvents(
        [Service] IMediator mediator,
        AuditAction? action,
        DateTime? since,
        int limit = 100,
        CancellationToken ct = default)
        => mediator.Send(new GetAuditEventsQuery(action, since, limit), ct);
}
