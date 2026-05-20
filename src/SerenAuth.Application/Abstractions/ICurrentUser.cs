using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.Abstractions;

/// <summary>
/// Caller context. Populated from the validated JWT in the API layer and
/// consumed by handlers to enforce per-tenant isolation. Handlers MUST
/// derive <c>organizationId</c> from this service — never from client input.
/// </summary>
public interface ICurrentUser
{
    string UserId { get; }
    string OrganizationId { get; }
    Role Role { get; }
    string IpAddress { get; }
    string CorrelationId { get; }
    bool IsAuthenticated { get; }
}
