using MongoDB.Driver;
using SerenAuth.Domain.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Infrastructure.Mongo;

public sealed class OrganizationRepository(MongoContext ctx) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(string id, CancellationToken ct) =>
        ctx.Organizations.Find(o => o.Id == id).FirstOrDefaultAsync(ct)!;
}

public sealed class UserRepository(MongoContext ctx) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        ctx.Users.Find(u => u.Email == email).FirstOrDefaultAsync(ct)!;

    public Task<User?> GetByIdAsync(string id, CancellationToken ct) =>
        ctx.Users.Find(u => u.Id == id).FirstOrDefaultAsync(ct)!;
}

public sealed class ProviderRepository(MongoContext ctx) : IProviderRepository
{
    public async Task<IReadOnlyList<Provider>> ListByOrganizationAsync(string organizationId, CancellationToken ct) =>
        await ctx.Providers.Find(p => p.OrganizationId == organizationId).ToListAsync(ct);

    public Task<Provider?> GetAsync(string organizationId, string providerId, CancellationToken ct) =>
        ctx.Providers.Find(p => p.OrganizationId == organizationId && p.Id == providerId)
            .FirstOrDefaultAsync(ct)!;
}

public sealed class PatientRepository(MongoContext ctx) : IPatientRepository
{
    public async Task<IReadOnlyList<Patient>> ListByOrganizationAsync(string organizationId, CancellationToken ct) =>
        await ctx.Patients.Find(p => p.OrganizationId == organizationId).ToListAsync(ct);

    public Task<Patient?> GetAsync(string organizationId, string patientId, CancellationToken ct) =>
        ctx.Patients.Find(p => p.OrganizationId == organizationId && p.Id == patientId)
            .FirstOrDefaultAsync(ct)!;
}

public sealed class PriorAuthorizationRepository(MongoContext ctx) : IPriorAuthorizationRepository
{
    public Task InsertAsync(PriorAuthorization pa, CancellationToken ct) =>
        ctx.PriorAuthorizations.InsertOneAsync(pa, cancellationToken: ct);

    public async Task UpdateAsync(PriorAuthorization pa, CancellationToken ct)
    {
        // Replace the document by id + org tuple to defend against cross-tenant writes.
        var filter = Builders<PriorAuthorization>.Filter.And(
            Builders<PriorAuthorization>.Filter.Eq(p => p.Id, pa.Id),
            Builders<PriorAuthorization>.Filter.Eq(p => p.OrganizationId, pa.OrganizationId));
        var result = await ctx.PriorAuthorizations.ReplaceOneAsync(filter, pa, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            throw new InvalidOperationException("PriorAuthorization not found or not owned by org.");
        }
    }

    public Task<PriorAuthorization?> GetAsync(string organizationId, string id, CancellationToken ct) =>
        ctx.PriorAuthorizations
            .Find(p => p.OrganizationId == organizationId && p.Id == id)
            .FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<PriorAuthorization>> ListAsync(
        string organizationId,
        PaStatus? status,
        string? payer,
        int limit,
        CancellationToken ct)
    {
        var filterBuilder = Builders<PriorAuthorization>.Filter;
        var filter = filterBuilder.Eq(p => p.OrganizationId, organizationId);
        if (status.HasValue)
        {
            filter &= filterBuilder.Eq(p => p.Status, status.Value);
        }
        if (!string.IsNullOrWhiteSpace(payer))
        {
            filter &= filterBuilder.Eq(p => p.Payer, payer);
        }

        return await ctx.PriorAuthorizations
            .Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Limit(limit)
            .ToListAsync(ct);
    }
}

public sealed class AuditEventRepository(MongoContext ctx) : IAuditEventRepository
{
    public Task InsertAsync(AuditEvent evt, CancellationToken ct) =>
        ctx.AuditEvents.InsertOneAsync(evt, cancellationToken: ct);

    public async Task<IReadOnlyList<AuditEvent>> ListByOrganizationAsync(
        string organizationId,
        AuditAction? action,
        DateTime? since,
        int limit,
        CancellationToken ct)
    {
        // Org filter is mandatory — never read across tenants. Uses the
        // existing ix_audit_org_ts index so this is a cheap newest-N read.
        var fb = Builders<AuditEvent>.Filter;
        var filter = fb.Eq(a => a.OrganizationId, organizationId);
        if (action.HasValue)
        {
            filter &= fb.Eq(a => a.Action, action.Value);
        }
        if (since.HasValue)
        {
            filter &= fb.Gte(a => a.Timestamp, since.Value);
        }

        return await ctx.AuditEvents
            .Find(filter)
            .SortByDescending(a => a.Timestamp)
            .Limit(limit)
            .ToListAsync(ct);
    }
}
