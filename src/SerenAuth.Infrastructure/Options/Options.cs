namespace SerenAuth.Infrastructure.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = "serenauth";
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; } = 60;
}

public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";
    public bool Enabled { get; set; }
}
