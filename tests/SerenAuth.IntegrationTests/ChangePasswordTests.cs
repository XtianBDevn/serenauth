using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;
using SerenAuth.Infrastructure.Mongo;
using Xunit;

namespace SerenAuth.IntegrationTests;

/// <summary>
/// End-to-end user-story tests for self-service password change.
///
/// User Story I — User changes their own password
///   As an authenticated clinician,
///   when I want to rotate the seeded "ChangeMe!123" credential,
///   I want to set a new password
///   so the old one stops working, the new one logs me in, and the
///   change is captured in the audit trail.
///
/// User Story J — Wrong current password is rejected
///   As an attacker who has somehow obtained a valid JWT (e.g. via a
///   stolen but not-yet-rotated bearer token),
///   when I attempt to change the user's password without knowing the
///   current password,
///   the mutation MUST fail with no state change and no successful
///   CHANGE_PASSWORD event in the audit log.
/// </summary>
public sealed class ChangePasswordTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private SerenAuthApiFactory _factory = null!;
    private HttpClient _userClient = null!;
    private string _orgId = string.Empty;
    private string _userId = string.Empty;
    private string _email = string.Empty;
    private const string InitialPassword = "ChangeMe!123";

    public ChangePasswordTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        _factory = new SerenAuthApiFactory(_mongo.ConnectionString);

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var org = new Organization { Name = "Riverbend Dialysis Center" };
            await ctx.Organizations.InsertOneAsync(org);
            _orgId = org.Id;

            var (hash, salt) = hasher.Hash(InitialPassword);
            var user = new User
            {
                OrganizationId = org.Id,
                Email = "clin@riverbend.example",
                DisplayName = "Dr. Mira Patel",
                Role = Role.Clinician
            };
            user.ChangePassword(hash, salt);
            await ctx.Users.InsertOneAsync(user);
            _userId = user.Id;
            _email = user.Email;
        }

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(_userId, _orgId, Role.Clinician));
    }

    public Task DisposeAsync()
    {
        _userClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName =
        "User Story I: User changes password → new one logs in, old one fails, CHANGE_PASSWORD audited")]
    public async Task User_can_change_their_own_password()
    {
        const string newPassword = "Sup3rSecret!Phrase";

        // GIVEN the user is signed in with their initial credential.
        // WHEN they call changePassword with the correct current password.
        var mutation = $$"""
            mutation {
              changePassword(input: {
                currentPassword: "{{InitialPassword}}",
                newPassword: "{{newPassword}}"
              }) {
                email
                changedAt
              }
            }
            """;
        var resp = await PostAsync(_userClient, mutation);

        // THEN the mutation succeeds.
        resp["errors"].Should().BeNull(because: $"changePassword failed: {resp}");
        resp["data"]!["changePassword"]!["email"]!.GetValue<string>().Should().Be(_email);

        // AND the old password no longer authenticates.
        var loginOld = await LoginAsync(_email, InitialPassword);
        loginOld["errors"].Should().NotBeNull(
            because: "the previous password must stop working after a successful rotation");

        // AND the new password does authenticate.
        var loginNew = await LoginAsync(_email, newPassword);
        loginNew["errors"].Should().BeNull(because: $"new password should log in: {loginNew}");
        loginNew["data"]!["login"]!["token"]!.GetValue<string>().Should().NotBeNullOrEmpty();

        // AND a CHANGE_PASSWORD event was recorded against this user.
        await AssertAuditedAsync(_userId, AuditAction.CHANGE_PASSWORD);
    }

    [Fact(DisplayName =
        "User Story J: Wrong current password rejected → no change, no CHANGE_PASSWORD event")]
    public async Task Changing_password_with_wrong_current_is_rejected()
    {
        const string newPassword = "Sup3rSecret!Phrase";

        // WHEN the user submits the wrong current password.
        var mutation = $$"""
            mutation {
              changePassword(input: {
                currentPassword: "this-is-not-the-password",
                newPassword: "{{newPassword}}"
              }) {
                email
              }
            }
            """;
        var resp = await PostAsync(_userClient, mutation);

        // THEN the mutation fails with a redacted error.
        resp["errors"].Should().NotBeNull(
            because: "wrong current-password must be rejected");

        // AND the original password still authenticates.
        var loginOriginal = await LoginAsync(_email, InitialPassword);
        loginOriginal["errors"].Should().BeNull(
            because: $"original password must still work: {loginOriginal}");

        // AND the would-be new password does NOT authenticate.
        var loginNew = await LoginAsync(_email, newPassword);
        loginNew["errors"].Should().NotBeNull(
            because: "a rejected change must not silently replace the credential");

        // AND no CHANGE_PASSWORD event leaked to the audit log.
        await AssertNotAuditedAsync(_userId, AuditAction.CHANGE_PASSWORD);
    }

    // ---------------- helpers ----------------

    private async Task<JsonNode> LoginAsync(string email, string password)
    {
        // login is public — no Authorization header needed; use a clean client.
        using var client = _factory.CreateClient();
        var mutation = $$"""
            mutation {
              login(input: { email: "{{email}}", password: "{{password}}" }) {
                token
                email
              }
            }
            """;
        return await PostAsync(client, mutation);
    }

    private async Task AssertAuditedAsync(string entityId, AuditAction action)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var events = await ctx.AuditEvents
            .Find(a => a.OrganizationId == _orgId && a.EntityId == entityId && a.Action == action)
            .ToListAsync();
        events.Should().NotBeEmpty(because: $"expected an {action} event for user {entityId}");
    }

    private async Task AssertNotAuditedAsync(string entityId, AuditAction action)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var events = await ctx.AuditEvents
            .Find(a => a.OrganizationId == _orgId && a.EntityId == entityId && a.Action == action)
            .ToListAsync();
        events.Should().BeEmpty(
            because: $"a rejected change-password must not emit an {action} event");
    }

    private static async Task<JsonNode> PostAsync(HttpClient client, string query)
    {
        var resp = await client.PostAsJsonAsync("/graphql", new { query });
        var body = await resp.Content.ReadAsStringAsync();
        return JsonNode.Parse(body) ?? throw new InvalidOperationException("Empty GraphQL response.");
    }

    private static string IssueToken(string userId, string orgId, Role role)
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SerenAuthApiFactory.TestJwtKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: SerenAuthApiFactory.TestJwtIssuer,
            audience: SerenAuthApiFactory.TestJwtAudience,
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Email, $"{role.ToString().ToLowerInvariant()}@test.example"),
                new("org", orgId),
                new(ClaimTypes.Role, role.ToString()),
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
