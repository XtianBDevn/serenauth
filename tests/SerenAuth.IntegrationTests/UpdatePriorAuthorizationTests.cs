using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
/// End-to-end user-story tests for the Edit Draft PA feature. As with
/// the decision tests, each fact reads as Given/When/Then so the test
/// name + assertions describe the user story without needing the body.
///
/// User Story C — Edit a draft
///   As an Intake user at Riverbend Dialysis Center,
///   when a clinician has flagged a wrong CPT on a draft PA,
///   I want to correct the procedure and payer
///   so the PA can be submitted with accurate clinical fields
///   and the edit is captured in the audit trail.
///
/// User Story D — Cannot edit once submitted
///   As an Intake user,
///   when a PA has already been submitted to the payer,
///   I must not be able to edit its clinical fields
///   so the payer always sees what was submitted, and no audit-bypass
///   path exists for rewriting history.
/// </summary>
public sealed class UpdatePriorAuthorizationTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private SerenAuthApiFactory _factory = null!;
    private HttpClient _intakeClient = null!;
    private HttpClient _clinicianClient = null!;
    private string _orgId = string.Empty;
    private string _patientId = string.Empty;
    private string _providerId = string.Empty;

    public UpdatePriorAuthorizationTests(MongoFixture mongo)
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
                ExternalMrn = "MRN-3001",
                FirstName = "Drew",
                LastName = "Demo",
                DateOfBirth = new DateOnly(1957, 11, 9)
            };
            await ctx.Patients.InsertOneAsync(patient);
            _patientId = patient.Id;

            var provider = new Provider
            {
                OrganizationId = org.Id,
                FirstName = "Ethan",
                LastName = "Brooks",
                Npi = "9876543210",
                Specialty = "Nephrology"
            };
            await ctx.Providers.InsertOneAsync(provider);
            _providerId = provider.Id;
        }

        _intakeClient = _factory.CreateClient();
        _intakeClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(_orgId, Role.Intake));

        _clinicianClient = _factory.CreateClient();
        _clinicianClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(_orgId, Role.Clinician));
    }

    public Task DisposeAsync()
    {
        _intakeClient.Dispose();
        _clinicianClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName =
        "User Story C: Intake edits a draft PA → fields change, status stays DRAFT, UPDATE_PA audited")]
    public async Task Intake_can_edit_a_draft_prior_authorization()
    {
        // GIVEN an existing draft PA created with the wrong CPT.
        var paId = await CreateDraftAsync(cpt: "90935", payer: "BCBS", confidence: 0.62);
        await AssertCurrentStateAsync(paId, expectedStatus: "DRAFT", expectedCpt: "90935");

        // WHEN the intake user edits the PA.
        var newCpt = "90937";
        var newPayer = "Aetna";
        var updateMutation = $$"""
            mutation {
              updatePriorAuthorization(input: {
                id: "{{paId}}",
                procedureCpt: "{{newCpt}}",
                diagnosisIcd10: "N18.6",
                payer: "{{newPayer}}",
                aiConfidence: 0.93
              }) {
                id
                procedureCpt
                payer
                aiConfidence
                status
              }
            }
            """;
        var resp = await PostAsync(_intakeClient, updateMutation);

        // THEN the mutation succeeds and returns the new fields.
        resp["errors"].Should().BeNull(because: $"update failed: {resp}");
        var data = resp["data"]!["updatePriorAuthorization"]!;
        data["procedureCpt"]!.GetValue<string>().Should().Be(newCpt);
        data["payer"]!.GetValue<string>().Should().Be(newPayer);
        data["status"]!.GetValue<string>().Should().Be("DRAFT");

        // AND the persisted record matches.
        await AssertCurrentStateAsync(paId, expectedStatus: "DRAFT", expectedCpt: newCpt);

        // AND the edit is in the audit log.
        await AssertAuditedAsync(paId, AuditAction.UPDATE_PA);
    }

    [Fact(DisplayName =
        "User Story D: Cannot edit a PA after it has been submitted — no field change, no UPDATE_PA audit")]
    public async Task Editing_a_submitted_prior_authorization_is_rejected()
    {
        // GIVEN a draft that the clinician has submitted.
        var paId = await CreateDraftAsync(cpt: "90935", payer: "BCBS", confidence: 0.81);
        await SubmitAsync(paId);
        await AssertCurrentStateAsync(paId, expectedStatus: "PENDING", expectedCpt: "90935");

        // WHEN intake tries to edit the now-submitted PA.
        var updateMutation = $$"""
            mutation {
              updatePriorAuthorization(input: {
                id: "{{paId}}",
                procedureCpt: "90937",
                diagnosisIcd10: "N18.6",
                payer: "Aetna",
                aiConfidence: 0.99
              }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_intakeClient, updateMutation);

        // THEN the mutation fails — the domain refuses to mutate a non-draft.
        resp["errors"].Should().NotBeNull(
            because: "the domain Update method rejects non-Draft PAs");
        resp["errors"]!.AsArray().Count.Should().BeGreaterThan(0);

        // AND the persisted PA is unchanged — fields stayed PENDING/90935.
        await AssertCurrentStateAsync(paId, expectedStatus: "PENDING", expectedCpt: "90935");

        // AND no UPDATE_PA event leaked to the audit log for this entity.
        await AssertNotAuditedAsync(paId, AuditAction.UPDATE_PA);
    }

    // ---------------- helpers ----------------

    private async Task<string> CreateDraftAsync(string cpt, string payer, double confidence)
    {
        var mutation = $$"""
            mutation {
              createPriorAuthorization(input: {
                patientId: "{{_patientId}}",
                providerId: "{{_providerId}}",
                procedureCpt: "{{cpt}}",
                diagnosisIcd10: "N18.6",
                payer: "{{payer}}",
                aiConfidence: {{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}}
              }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_intakeClient, mutation);
        resp["errors"].Should().BeNull(because: $"createPriorAuthorization failed: {resp}");
        return resp["data"]!["createPriorAuthorization"]!["id"]!.GetValue<string>();
    }

    private async Task SubmitAsync(string id)
    {
        // Submit requires Clinician/Admin — RequirePaSubmit.
        var mutation = $$"""
            mutation {
              submitPriorAuthorization(input: { id: "{{id}}" }) {
                id
                status
              }
            }
            """;
        var resp = await PostAsync(_clinicianClient, mutation);
        resp["errors"].Should().BeNull(because: $"submitPriorAuthorization failed: {resp}");
    }

    private async Task AssertCurrentStateAsync(string id, string expectedStatus, string expectedCpt)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var pa = await ctx.PriorAuthorizations.Find(p => p.Id == id).FirstOrDefaultAsync();
        pa.Should().NotBeNull();
        pa!.Status.ToString().ToUpperInvariant().Should().Be(expectedStatus);
        pa.ProcedureCpt.Should().Be(expectedCpt);
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
            because: $"a rejected update must not emit an {action} event for PA {entityId}");
    }

    private static async Task<JsonNode> PostAsync(HttpClient client, string query)
    {
        var resp = await client.PostAsJsonAsync("/graphql", new { query });
        var body = await resp.Content.ReadAsStringAsync();
        return JsonNode.Parse(body) ?? throw new InvalidOperationException("Empty GraphQL response.");
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
