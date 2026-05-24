using MediatR;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.Auditing;

/// <summary>
/// Admin-only read of the append-only audit log, scoped to the caller's
/// organization. Optional filters narrow by action or time. The handler
/// enforces tenant isolation; the GraphQL policy enforces role.
/// </summary>
public sealed record GetAuditEventsQuery(
    AuditAction? Action,
    DateTime? Since,
    int Limit = 100) : IRequest<IReadOnlyList<AuditEventDto>>;

public sealed record AuditEventDto(
    string Id,
    DateTime Timestamp,
    AuditAction Action,
    string Entity,
    string EntityId,
    string UserId,
    string OrganizationId,
    string CorrelationId)
{
    public static AuditEventDto FromEntity(AuditEvent e) => new(
        e.Id,
        e.Timestamp,
        e.Action,
        e.Entity,
        e.EntityId,
        e.UserId,
        e.OrganizationId,
        e.CorrelationId);
}
