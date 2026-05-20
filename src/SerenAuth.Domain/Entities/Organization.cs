namespace SerenAuth.Domain.Entities;

/// <summary>
/// Tenant boundary. Every other entity (User, Patient, Provider, PA,
/// AuditEvent) is scoped to an Organization. Cross-tenant access is
/// rejected at the application layer.
/// </summary>
public sealed class Organization
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
