using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Infrastructure.Security;

/// <summary>
/// Concrete audit publisher. Uses the caller's <see cref="ICurrentUser"/>
/// to stamp every event with userId, organizationId, IP, and correlation
/// ID, then inserts via the repository's append-only API.
/// </summary>
public sealed class AuditPublisher(IAuditEventRepository repo, ICurrentUser caller) : IAuditPublisher
{
    public Task PublishAsync(AuditAction action, string entity, string entityId, CancellationToken ct)
    {
        var evt = new AuditEvent
        {
            Action = action,
            Entity = entity,
            EntityId = entityId,
            UserId = caller.UserId,
            OrganizationId = caller.OrganizationId,
            IpAddress = caller.IpAddress,
            CorrelationId = caller.CorrelationId
        };
        return repo.InsertAsync(evt, ct);
    }
}
