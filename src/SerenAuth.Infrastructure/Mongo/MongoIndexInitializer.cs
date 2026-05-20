using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using SerenAuth.Domain.Entities;

namespace SerenAuth.Infrastructure.Mongo;

/// <summary>
/// Creates the indexes the application relies on. Indexes are idempotent
/// (Mongo no-ops on duplicate definitions) so this runs safely on every
/// boot.
/// </summary>
public sealed class MongoIndexInitializer(MongoContext ctx) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureIndexAsync(
            ctx.Users,
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            unique: true,
            name: "ix_users_email_unique",
            cancellationToken);

        await EnsureIndexAsync(
            ctx.Users,
            Builders<User>.IndexKeys.Ascending(u => u.OrganizationId),
            name: "ix_users_org",
            ct: cancellationToken);

        await EnsureIndexAsync(
            ctx.Providers,
            Builders<Provider>.IndexKeys.Ascending(p => p.OrganizationId),
            name: "ix_providers_org",
            ct: cancellationToken);

        await EnsureIndexAsync(
            ctx.Patients,
            Builders<Patient>.IndexKeys.Ascending(p => p.OrganizationId),
            name: "ix_patients_org",
            ct: cancellationToken);

        await EnsureIndexAsync(
            ctx.PriorAuthorizations,
            Builders<PriorAuthorization>.IndexKeys
                .Ascending(p => p.OrganizationId)
                .Ascending(p => p.Status)
                .Descending(p => p.CreatedAt),
            name: "ix_pa_org_status_createdAt",
            ct: cancellationToken);

        await EnsureIndexAsync(
            ctx.PriorAuthorizations,
            Builders<PriorAuthorization>.IndexKeys
                .Ascending(p => p.OrganizationId)
                .Ascending(p => p.Payer),
            name: "ix_pa_org_payer",
            ct: cancellationToken);

        await EnsureIndexAsync(
            ctx.AuditEvents,
            Builders<AuditEvent>.IndexKeys
                .Ascending(a => a.OrganizationId)
                .Descending(a => a.Timestamp),
            name: "ix_audit_org_ts",
            ct: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Task EnsureIndexAsync<T>(
        IMongoCollection<T> coll,
        IndexKeysDefinition<T> keys,
        string name,
        CancellationToken ct,
        bool unique = false)
    {
        var model = new CreateIndexModel<T>(keys, new CreateIndexOptions { Name = name, Unique = unique });
        return coll.Indexes.CreateOneAsync(model, cancellationToken: ct);
    }
}
