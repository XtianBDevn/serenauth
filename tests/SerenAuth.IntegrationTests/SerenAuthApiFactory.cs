using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SerenAuth.IntegrationTests;

/// <summary>
/// Boots the real API host but overrides the Mongo connection to point
/// at the test container, and supplies a deterministic JWT signing key.
/// </summary>
public sealed class SerenAuthApiFactory(string mongoConnectionString)
    : WebApplicationFactory<Program>
{
    public const string TestJwtIssuer = "serenauth.tests";
    public const string TestJwtAudience = "serenauth.tests.web";
    public static readonly string TestJwtKey =
        "test-signing-key-please-replace-test-signing-key-please-replace";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = mongoConnectionString,
                ["Mongo:Database"] = $"serenauth_test_{Guid.NewGuid():N}",
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:SigningKey"] = TestJwtKey,
                ["Seeding:Enabled"] = "false",
                ["Cors:AllowedOrigins"] = "http://localhost:3000"
            });
        });
    }
}
