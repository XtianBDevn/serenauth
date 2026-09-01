using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.Abstractions;

/// <summary>
/// Application-facing audit sink. Wraps the repository so handlers don't
/// need to construct AuditEvent objects directly.
/// </summary>
public interface IAuditPublisher
{
    Task PublishAsync(AuditAction action, string entity, string entityId, CancellationToken ct);
}
