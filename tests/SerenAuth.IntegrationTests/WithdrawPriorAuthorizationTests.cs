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
/// End-to-end user-story tests for the Withdraw PA feature.
///
/// User Story G — Clinician withdraws a pending PA
///   As a Clinician at Riverbend Dialysis Center,
///   when I realise a freshly-submitted PA referenced the wrong patient,
///   I want to withdraw it before the payer responds
///   so the PA closes as WITHDRAWN (not Approved/Denied) and the
///   withdrawal is captured in the audit trail.
///
/// User Story H — Cannot withdraw a terminal PA
///   As a Clinician,
///   when a PA has already been Approved (or Denied) by the payer,
///   I must not be able to withdraw it
///   so terminal states stay terminal and the audit history cannot
///   be muddied retroactively.
/// </summary>
public sealed class WithdrawPriorAuthorizationTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private SerenAuthApiFactory _factory = null!;
    private HttpClient _clinicianClient = null!;
    private HttpClient _adminClient = null!;
    private string _orgId = string.Empty;
    private string _patientId = string.Empty;
    private string _providerId = string.Empty;

    public WithdrawPriorAuthorizationTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        _factory = new SerenAuthApiFactory(_mongo.ConnectionString);

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();

            var org = new Organization { Name = "Riverbend Dialysis Center" };
            await ctx.Organizations.InsertOneAsync(org);
            _orgId = org.Id;

            var patient = new Patient
            {
                OrganizationId = org.Id,
                ExternalMrn = "MRN-W-1",
                FirstName = "Rowan",
                LastName = "Demo",
                DateOfBirth = new DateOnly(1958, 6, 22)
            };
            await ctx.Patients.InsertOneAsync(patient);
            _patientId = patient.Id;

            var provider = new Provider
            {
                OrganizationId = org.Id,
                FirstName = "Mira",
                LastName = "Patel",
                Npi = "1234567890",
                Specialty = "Nephrology"
            };
            await ctx.Providers.InsertOneAsync(provider);
            _providerId = provider.Id;
        }

        _clinicianClient = ClientFor(_orgId, Role.Clinician);
        _adminClient = ClientFor(_orgId, Role.Admin);
    }

    public Task DisposeAsync()
    {
        _clinicianClient.Dispose();
        _adminClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName =
        "User Story G: Clinician withdraws a pending PA → status WITHDRAWN + WITHDRAW_PA audited")]
    public async Task Clinician_can_withdraw_a_pending_prior_authorization()
    {
        // GIVEN the clinician has created and submitted a draft PA.
        var paId = await CreateDraftAsync();
        await SubmitAsync(paId);
        await AssertCurrentStatusAsync(paId, "PENDING");

        // WHEN the clinician withdraws the PA before the payer responds.
        var mutation = $$"""
            mutation {
              withdrawPriorAuthorization(input: { id: "{{paId}}" }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);

        // THEN the mutation returns WITHDRAWN.
        resp["errors"].Should().BeNull(because: $"withdraw failed: {resp}");
        resp["data"]!["withdrawPriorAuthorization"]!["status"]!.GetValue<string>()
            .Should().Be("WITHDRAWN");

        // AND the persisted PA reflects the new status.
        await AssertCurrentStatusAsync(paId, "WITHDRAWN");

        // AND the withdrawal is in the audit log.
        await AssertAuditedAsync(paId, AuditAction.WITHDRAW_PA);
    }

    [Fact(DisplayName =
        "User Story H: Cannot withdraw an Approved PA — terminal state holds, no WITHDRAW_PA audit")]
    public async Task Withdrawing_an_approved_prior_authorization_is_rejected()
    {
        // GIVEN a PA that has been submitted and approved by the admin.
        var paId = await CreateDraftAsync();
        await SubmitAsync(paId);
        await DecideAsync(paId, "APPROVE");
        await AssertCurrentStatusAsync(paId, "APPROVED");

        // WHEN the clinician tries to withdraw the now-approved PA.
        var mutation = $$"""
            mutation {
              withdrawPriorAuthorization(input: { id: "{{paId}}" }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);

        // THEN the mutation fails — the domain refuses non-Pending withdraws.
        resp["errors"].Should().NotBeNull(
            because: "the domain Withdraw method rejects non-Pending PAs");

        // AND the persisted PA is still APPROVED — no state change leaked.
        await AssertCurrentStatusAsync(paId, "APPROVED");

        // AND no WITHDRAW_PA event was recorded.
        await AssertNotAuditedAsync(paId, AuditAction.WITHDRAW_PA);
    }

    // ---------------- helpers ----------------

    private async Task<string> CreateDraftAsync()
    {
        var mutation = $$"""
            mutation {
              createPriorAuthorization(input: {
                patientId: "{{_patientId}}",
                providerId: "{{_providerId}}",
                procedureCpt: "90935",
                diagnosisIcd10: "N18.6",
                payer: "BCBS",
                aiConfidence: 0.88
              }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);
        resp["errors"].Should().BeNull(because: $"create failed: {resp}");
        return resp["data"]!["createPriorAuthorization"]!["id"]!.GetValue<string>();
    }

    private async Task SubmitAsync(string id)
    {
        var mutation = $$"""
            mutation {
              submitPriorAuthorization(input: { id: "{{id}}" }) { id status }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);
        resp["errors"].Should().BeNull(because: $"submit failed: {resp}");
    }

    private async Task DecideAsync(string id, string decision)
    {
        var mutation = $$"""
            mutation {
              decidePriorAuthorization(input: { id: "{{id}}", decision: {{decision}} }) {
                id status
              }
            }
            """;
        var resp = await PostAsync(_adminClient, mutation);
        resp["errors"].Should().BeNull(because: $"decide failed: {resp}");
    }

    private async Task AssertCurrentStatusAsync(string id, string expectedStatus)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var pa = await ctx.PriorAuthorizations.Find(p => p.Id == id).FirstOrDefaultAsync();
        pa.Should().NotBeNull();
        pa!.Status.ToString().ToUpperInvariant().Should().Be(expectedStatus);
    }

    private async Task AssertAuditedAsync(string entityId, AuditAction action)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var events = await ctx.AuditEvents
            .Find(a => a.OrganizationId == _orgId && a.EntityId == entityId)
            .ToListAsync();
        events.Should().Contain(a => a.Action == action,
            because: $"expected an {action} event for PA {entityId}");
    }

    private async Task AssertNotAuditedAsync(string entityId, AuditAction action)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var events = await ctx.AuditEvents
            .Find(a => a.OrganizationId == _orgId && a.EntityId == entityId && a.Action == action)
            .ToListAsync();
        events.Should().BeEmpty(
            because: $"a rejected withdraw must not emit a {action} event for PA {entityId}");
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
