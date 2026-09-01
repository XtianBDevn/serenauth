using SerenAuth.Domain.Enums;

namespace SerenAuth.Domain.Entities;

/// <summary>
/// Append-only audit record. The persistence layer must never expose
/// an update or delete operation against this collection. Every field
/// is required so an event is self-describing for compliance review.
/// </summary>
public sealed class AuditEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string UserId { get; init; } = string.Empty;
    public string OrganizationId { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public AuditAction Action { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
