using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Domain.Abstractions;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(string id, CancellationToken ct);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(string id, CancellationToken ct);
}

public interface IProviderRepository
{
    Task<IReadOnlyList<Provider>> ListByOrganizationAsync(string organizationId, CancellationToken ct);
    Task<Provider?> GetAsync(string organizationId, string providerId, CancellationToken ct);
}

public interface IPatientRepository
{
    Task<IReadOnlyList<Patient>> ListByOrganizationAsync(string organizationId, CancellationToken ct);
    Task<Patient?> GetAsync(string organizationId, string patientId, CancellationToken ct);
}

public interface IPriorAuthorizationRepository
{
    Task InsertAsync(PriorAuthorization pa, CancellationToken ct);
    Task UpdateAsync(PriorAuthorization pa, CancellationToken ct);
    Task<PriorAuthorization?> GetAsync(string organizationId, string id, CancellationToken ct);
    Task<IReadOnlyList<PriorAuthorization>> ListAsync(
        string organizationId,
        PaStatus? status,
        string? payer,
        int limit,
        CancellationToken ct);
}

/// <summary>
/// Append-only. No update or delete is exposed by design.
/// </summary>
public interface IAuditEventRepository
{
    Task InsertAsync(AuditEvent evt, CancellationToken ct);
}
