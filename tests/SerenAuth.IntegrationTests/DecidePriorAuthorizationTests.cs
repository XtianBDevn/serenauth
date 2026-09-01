using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
/// End-to-end user-story tests for the PA decision feature. Each fact is
/// written as a Given/When/Then narrative so a non-engineer reading the
/// test name + assertion order can recover the user story it covers.
///
/// User Story A — Approve
///   As an Admin at Riverbend Dialysis Center,
///   when a clinician has submitted a draft prior authorization,
///   I want to record the payer's approval
///   so the PA closes as APPROVED and the decision is auditable.
///
/// User Story B — Deny
///   As an Admin,
///   when a clinician has submitted a draft prior authorization,
///   and the payer denies it,
///   I want to record the denial
///   so the PA closes as DENIED and the decision is auditable.
///
/// Both stories exercise the full HTTP → GraphQL → MediatR → Mongo
/// pipeline using a real Mongo container.
/// </summary>
public sealed class DecidePriorAuthorizationTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private SerenAuthApiFactory _factory = null!;
    private HttpClient _clinicianClient = null!;
    private HttpClient _adminClient = null!;
    private string _orgId = string.Empty;
    private string _patientId = string.Empty;
    private string _providerId = string.Empty;

    public DecidePriorAuthorizationTests(MongoFixture mongo)
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
                ExternalMrn = "MRN-2001",
                FirstName = "Casey",
                LastName = "Demo",
                DateOfBirth = new DateOnly(1959, 3, 14)
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

        _clinicianClient = _factory.CreateClient();
        _clinicianClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(_orgId, Role.Clinician));

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(_orgId, Role.Admin));
    }

    public Task DisposeAsync()
    {
        _clinicianClient.Dispose();
        _adminClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName =
        "User Story A: Admin approves a pending PA → status APPROVED + DECIDE_PA audit event")]
    public async Task Admin_can_approve_a_pending_prior_authorization()
    {
        // GIVEN the clinician has created and submitted a draft PA.
        var paId = await CreateDraftAsync();
        await SubmitAsync(paId);
        await AssertCurrentStatusAsync(paId, "PENDING");

        // WHEN the admin records the payer's APPROVE decision.
        var decideMutation = $$"""
            mutation {
              decidePriorAuthorization(input: {
                id: "{{paId}}",
                decision: APPROVE
              }) {
                id
                status
              }
            }
            """;
        var decideResp = await PostAsync(_adminClient, decideMutation);

        // THEN the mutation returns APPROVED.
        decideResp.TryGetProperty("errors", out _).Should().BeFalse(
            because: $"decide mutation failed: {decideResp.GetRawText()}");
        decideResp.GetProperty("data").GetProperty("decidePriorAuthorization")
            .GetProperty("status").GetString().Should().Be("APPROVED");

        // AND the persisted PA reflects the new status.
        await AssertCurrentStatusAsync(paId, "APPROVED");

        // AND the decision was recorded in the audit log.
        await AssertAuditedAsync(paId, AuditAction.DECIDE_PA);
    }

    [Fact(DisplayName =
        "User Story B: Admin denies a pending PA → status DENIED + DECIDE_PA audit event")]
    public async Task Admin_can_deny_a_pending_prior_authorization()
    {
        // GIVEN the clinician has created and submitted a draft PA.
        var paId = await CreateDraftAsync();
        await SubmitAsync(paId);
        await AssertCurrentStatusAsync(paId, "PENDING");

        // WHEN the admin records the payer's DENY decision.
        var decideMutation = $$"""
            mutation {
              decidePriorAuthorization(input: {
                id: "{{paId}}",
                decision: DENY
              }) {
                id
                status
              }
            }
            """;
        var decideResp = await PostAsync(_adminClient, decideMutation);

        // THEN the mutation returns DENIED.
        decideResp.TryGetProperty("errors", out _).Should().BeFalse(
            because: $"decide mutation failed: {decideResp.GetRawText()}");
        decideResp.GetProperty("data").GetProperty("decidePriorAuthorization")
            .GetProperty("status").GetString().Should().Be("DENIED");

        await AssertCurrentStatusAsync(paId, "DENIED");
        await AssertAuditedAsync(paId, AuditAction.DECIDE_PA);
    }

    [Fact(DisplayName =
        "Guardrail: a clinician (non-admin) cannot decide a PA — RequireAdmin policy holds")]
    public async Task Clinician_cannot_decide_a_prior_authorization()
    {
        var paId = await CreateDraftAsync();
        await SubmitAsync(paId);

        var decideMutation = $$"""
            mutation {
              decidePriorAuthorization(input: {
                id: "{{paId}}",
                decision: APPROVE
              }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, decideMutation);

        // HotChocolate surfaces policy failures as an "errors" array.
        resp.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);

        // And the PA is still PENDING — no state change leaked through.
        await AssertCurrentStatusAsync(paId, "PENDING");
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
                aiConfidence: 0.91
              }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);
        resp.TryGetProperty("errors", out _).Should().BeFalse(
            because: $"createPriorAuthorization failed: {resp.GetRawText()}");
        return resp.GetProperty("data")
            .GetProperty("createPriorAuthorization")
            .GetProperty("id")
            .GetString()!;
    }

    private async Task SubmitAsync(string id)
    {
        var mutation = $$"""
            mutation {
              submitPriorAuthorization(input: { id: "{{id}}" }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);
        resp.TryGetProperty("errors", out _).Should().BeFalse(
            because: $"submitPriorAuthorization failed: {resp.GetRawText()}");
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

    private static async Task<JsonElement> PostAsync(HttpClient client, string query)
    {
        var resp = await client.PostAsJsonAsync("/graphql", new { query });
        var body = await resp.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
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
