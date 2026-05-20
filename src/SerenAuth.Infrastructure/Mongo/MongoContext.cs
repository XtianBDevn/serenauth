using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using SerenAuth.Domain.Entities;
using SerenAuth.Infrastructure.Options;

namespace SerenAuth.Infrastructure.Mongo;

/// <summary>
/// Centralized MongoDB access. Owns the <see cref="MongoClient"/> for
/// connection-pool reuse and exposes typed collections used by the
/// repositories.
/// </summary>
public sealed class MongoContext
{
    public IMongoDatabase Database { get; }

    public IMongoCollection<Organization> Organizations =>
        Database.GetCollection<Organization>("organizations");

    public IMongoCollection<User> Users =>
        Database.GetCollection<User>("users");

    public IMongoCollection<Provider> Providers =>
        Database.GetCollection<Provider>("providers");

    public IMongoCollection<Patient> Patients =>
        Database.GetCollection<Patient>("patients");

    public IMongoCollection<PriorAuthorization> PriorAuthorizations =>
        Database.GetCollection<PriorAuthorization>("prior_authorizations");

    public IMongoCollection<AuditEvent> AuditEvents =>
        Database.GetCollection<AuditEvent>("audit_events");

    static MongoContext()
    {
        // Use camelCase keys in Mongo while keeping PascalCase in C#. Stable
        // serialization is important for the audit collection's immutability.
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(MongoDB.Bson.BsonType.String),
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("SerenAuthConventions", pack, _ => true);
    }

    public MongoContext(IOptions<MongoOptions> options)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            throw new InvalidOperationException("Mongo:ConnectionString is required.");
        }
        var client = new MongoClient(opts.ConnectionString);
        Database = client.GetDatabase(opts.Database);
    }
}
