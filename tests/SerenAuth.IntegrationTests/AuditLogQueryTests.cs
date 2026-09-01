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
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;
using SerenAuth.Infrastructure.Mongo;
using Xunit;

namespace SerenAuth.IntegrationTests;

/// <summary>
/// End-to-end user-story tests for the Audit Log Query feature.
///
/// User Story E — Admin can read their org's audit log
///   As an Admin at Riverbend Dialysis Center,
///   when a clinician has created, edited, submitted, and I have decided
///   a prior authorization,
///   I want to read the audit trail for my organization
///   so I can produce a compliance review without shell access to Mongo.
///
/// User Story F — Cross-tenant isolation
///   As an Admin at Org A,
///   when Org B has its own activity in the system,
///   I must not see any of Org B's audit events
///   so tenant isolation holds even for the most privileged role.
/// </summary>
public sealed class AuditLogQueryTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private SerenAuthApiFactory _factory = null!;

    // --- Org A (Riverbend) ---
    private HttpClient _orgAAdmin = null!;
    private HttpClient _orgAIntake = null!;
    private HttpClient _orgAClinician = null!;
    private string _orgAId = string.Empty;
    private string _orgAPatientId = string.Empty;
    private string _orgAProviderId = string.Empty;

    // --- Org B (used only by Story F) ---
    private HttpClient _orgBAdmin = null!;
    private HttpClient _orgBIntake = null!;
    private string _orgBId = string.Empty;
    private string _orgBPatientId = string.Empty;
    private string _orgBProviderId = string.Empty;

    public AuditLogQueryTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        _factory = new SerenAuthApiFactory(_mongo.ConnectionString);

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();

            var orgA = new Organization { Name = "Riverbend Dialysis Center" };
            var orgB = new Organization { Name = "Lakeside Renal Group" };
            await ctx.Organizations.InsertManyAsync(new[] { orgA, orgB });
            _orgAId = orgA.Id;
            _orgBId = orgB.Id;

            var (paId, prvId) = await SeedOrgAsync(ctx, orgA.Id, "MRN-A-1");
            _orgAPatientId = paId;
            _orgAProviderId = prvId;

            var (pbId, prvBid) = await SeedOrgAsync(ctx, orgB.Id, "MRN-B-1");
            _orgBPatientId = pbId;
            _orgBProviderId = prvBid;
        }

        _orgAAdmin = ClientFor(_orgAId, Role.Admin);
        _orgAIntake = ClientFor(_orgAId, Role.Intake);
        _orgAClinician = ClientFor(_orgAId, Role.Clinician);

        _orgBAdmin = ClientFor(_orgBId, Role.Admin);
        _orgBIntake = ClientFor(_orgBId, Role.Intake);
    }

    public Task DisposeAsync()
    {
        _orgAAdmin.Dispose();
        _orgAIntake.Dispose();
        _orgAClinician.Dispose();
        _orgBAdmin.Dispose();
        _orgBIntake.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName =
        "User Story E: Admin reads their org's audit trail — sees CREATE_PA, UPDATE_PA, SUBMIT_PA, DECIDE_PA")]
    public async Task Admin_can_read_their_organizations_audit_trail()
    {
        // GIVEN a full PA lifecycle in Org A: intake creates + edits, clinician
        // submits, admin approves. Each step writes one audit event.
        var paId = await CreateDraftAsync(_orgAIntake, _orgAPatientId, _orgAProviderId);
        await EditDraftAsync(_orgAIntake, paId);
        await SubmitAsync(_orgAClinician, paId);
        await DecideAsync(_orgAAdmin, paId, "APPROVE");

        // WHEN the admin queries the audit log.
        var query = """
            query {
              auditEvents(limit: 50) {
                id
                action
                entity
                entityId
                organizationId
                timestamp
              }
            }
            """;
        var resp = await PostAsync(_orgAAdmin, query);

        // THEN the response is a clean array with no errors.
        resp["errors"].Should().BeNull(because: $"audit query failed: {resp}");
        var rows = resp["data"]!["auditEvents"]!.AsArray();
        rows.Count.Should().BeGreaterThanOrEqualTo(4);

        // AND every event we just generated is present, scoped to Org A.
        var forThisPa = rows
            .Where(r => r!["entityId"]!.GetValue<string>() == paId)
            .Select(r => r!["action"]!.GetValue<string>())
            .ToHashSet();
        forThisPa.Should().Contain(new[] { "CREATE_PA", "UPDATE_PA", "SUBMIT_PA", "DECIDE_PA" });

        // AND every returned event is org-scoped.
        rows.Select(r => r!["organizationId"]!.GetValue<string>())
            .Should().OnlyContain(orgId => orgId == _orgAId);

        // AND results are ordered newest-first.
        var timestamps = rows
            .Select(r => DateTime.Parse(r!["timestamp"]!.GetValue<string>(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind))
            .ToList();
        timestamps.Should().BeInDescendingOrder();
    }

    [Fact(DisplayName =
        "User Story F: Admin at Org A cannot see Org B's audit events — cross-tenant isolation holds")]
    public async Task Admin_cannot_see_another_organizations_audit_events()
    {
        // GIVEN activity in BOTH orgs.
        var orgAPaId = await CreateDraftAsync(_orgAIntake, _orgAPatientId, _orgAProviderId);
        await SubmitAsync(_orgAClinician, orgAPaId);

        var orgBPaId = await CreateDraftAsync(_orgBIntake, _orgBPatientId, _orgBProviderId);

        // WHEN Org A's admin reads the audit log.
        var query = """
            query {
              auditEvents(limit: 200) {
                action
                entityId
                organizationId
              }
            }
            """;
        var resp = await PostAsync(_orgAAdmin, query);
        resp["errors"].Should().BeNull(because: $"audit query failed: {resp}");
        var rows = resp["data"]!["auditEvents"]!.AsArray();

        // THEN every event belongs to Org A.
        rows.Select(r => r!["organizationId"]!.GetValue<string>())
            .Should().OnlyContain(orgId => orgId == _orgAId);

        // AND Org B's PA id never appears as an entityId in Org A's view.
        rows.Select(r => r!["entityId"]!.GetValue<string>())
            .Should().NotContain(orgBPaId);

        // AND the inverse holds: Org B's admin sees their own activity but not Org A's.
        var respB = await PostAsync(_orgBAdmin, query);
        var rowsB = respB["data"]!["auditEvents"]!.AsArray();
        rowsB.Select(r => r!["organizationId"]!.GetValue<string>())
            .Should().OnlyContain(orgId => orgId == _orgBId);
        rowsB.Select(r => r!["entityId"]!.GetValue<string>())
            .Should().NotContain(orgAPaId);
    }

    // ---------------- seeding helpers ----------------

    private static async Task<(string patientId, string providerId)> SeedOrgAsync(
        MongoContext ctx, string orgId, string mrn)
    {
        var patient = new Patient
        {
            OrganizationId = orgId,
            ExternalMrn = mrn,
            FirstName = "Patient",
            LastName = "Demo",
            DateOfBirth = new DateOnly(1960, 1, 1)
        };
        await ctx.Patients.InsertOneAsync(patient);

        var provider = new Provider
        {
            OrganizationId = orgId,
            FirstName = "Mira",
            LastName = "Patel",
            // NPI uniqueness isn't enforced by index in the test DB; differentiate to avoid surprises.
            Npi = orgId[..10],
            Specialty = "Nephrology"
        };
        await ctx.Providers.InsertOneAsync(provider);

        return (patient.Id, provider.Id);
    }

    // ---------------- GraphQL helpers ----------------

    private static async Task<string> CreateDraftAsync(HttpClient client, string patientId, string providerId)
    {
        var mutation = $$"""
            mutation {
              createPriorAuthorization(input: {
                patientId: "{{patientId}}",
                providerId: "{{providerId}}",
                procedureCpt: "90935",
                diagnosisIcd10: "N18.6",
                payer: "BCBS",
                aiConfidence: 0.85
              }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(client, mutation);
        resp["errors"].Should().BeNull(because: $"create failed: {resp}");
        return resp["data"]!["createPriorAuthorization"]!["id"]!.GetValue<string>();
    }

    private static async Task EditDraftAsync(HttpClient client, string paId)
    {
        var mutation = $$"""
            mutation {
              updatePriorAuthorization(input: {
                id: "{{paId}}",
                procedureCpt: "90937",
                diagnosisIcd10: "N18.6",
                payer: "Aetna",
                aiConfidence: 0.91
              }) { id status }
            }
            """;
        var resp = await PostAsync(client, mutation);
        resp["errors"].Should().BeNull(because: $"update failed: {resp}");
    }

    private static async Task SubmitAsync(HttpClient client, string paId)
    {
        var mutation = $$"""
            mutation {
              submitPriorAuthorization(input: { id: "{{paId}}" }) { id status }
            }
            """;
        var resp = await PostAsync(client, mutation);
        resp["errors"].Should().BeNull(because: $"submit failed: {resp}");
    }

    private static async Task DecideAsync(HttpClient client, string paId, string decision)
    {
        var mutation = $$"""
            mutation {
              decidePriorAuthorization(input: { id: "{{paId}}", decision: {{decision}} }) {
                id status
              }
            }
            """;
        var resp = await PostAsync(client, mutation);
        resp["errors"].Should().BeNull(because: $"decide failed: {resp}");
    }

    private static async Task<JsonNode> PostAsync(HttpClient client, string query)
    {
        var resp = await client.PostAsJsonAsync("/graphql", new { query });
        var body = await resp.Content.ReadAsStringAsync();
        return JsonNode.Parse(body) ?? throw new InvalidOperationException("Empty GraphQL response.");
    }

    private HttpClient ClientFor(string orgId, Role role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(orgId, role));
        return client;
    }

    private static string IssueToken(string orgId, Role role)
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SerenAuthApiFactory.TestJwtKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: SerenAuthApiFactory.TestJwtIssuer,
            audience: SerenAuthApiFactory.TestJwtAudience,
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString("N")),
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
