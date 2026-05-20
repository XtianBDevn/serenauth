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

public sealed class PriorAuthorizationsTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private readonly MongoFixture _mongo;
    private SerenAuthApiFactory _factory = null!;
    private HttpClient _client = null!;
    private string _orgId = string.Empty;
    private string _patientId = string.Empty;
    private string _providerId = string.Empty;

    public PriorAuthorizationsTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    public async Task InitializeAsync()
    {
        _factory = new SerenAuthApiFactory(_mongo.ConnectionString);
        _client = _factory.CreateClient();

        // Seed an org + patient + provider directly through MongoContext so
        // the GraphQL flow can satisfy its per-tenant guards.
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();

        var org = new Organization { Name = "Test Clinic" };
        await ctx.Organizations.InsertOneAsync(org);
        _orgId = org.Id;

        var patient = new Patient
        {
            OrganizationId = org.Id,
            ExternalMrn = "MRN-0001",
            FirstName = "Alex",
            LastName = "Demo",
            DateOfBirth = new DateOnly(1962, 7, 4)
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

        // Attach a clinician JWT so the resolver passes authorization.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueClinicianToken(org.Id));
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_then_list_emits_audit_events()
    {
        var createMutation = $$"""
            mutation {
              createPriorAuthorization(input: {
                patientId: "{{_patientId}}",
                providerId: "{{_providerId}}",
                procedureCpt: "90935",
                diagnosisIcd10: "N18.6",
                payer: "BCBS",
                aiConfidence: 0.87
              }) {
                id
                status
                payer
              }
            }
            """;

        var createResp = await PostAsync(createMutation);
        createResp["data"]!["createPriorAuthorization"]!["status"]!.GetString()
            .Should().Be("DRAFT");

        var listQuery = """
            query {
              priorAuthorizations(limit: 10) {
                id
                payer
                status
              }
            }
            """;

        var listResp = await PostAsync(listQuery);
        var rows = listResp["data"]!["priorAuthorizations"]!.AsArray();
        rows.Count.Should().Be(1);

        // Audit trail should record both CREATE_PA and VIEW_PA.
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var audits = await ctx.AuditEvents
            .Find(a => a.OrganizationId == _orgId)
            .ToListAsync();
        audits.Should().Contain(a => a.Action == AuditAction.CREATE_PA);
        audits.Should().Contain(a => a.Action == AuditAction.VIEW_PA);
    }

    [Fact]
    public async Task Create_rejects_off_domain_cpt()
    {
        var mutation = $$"""
            mutation {
              createPriorAuthorization(input: {
                patientId: "{{_patientId}}",
                providerId: "{{_providerId}}",
                procedureCpt: "99999",
                diagnosisIcd10: "N18.6",
                payer: "BCBS",
                aiConfidence: 0.5
              }) {
                id
              }
            }
            """;

        var resp = await PostAsync(mutation);
        resp["errors"].Should().NotBeNull();
    }

    private async Task<JsonElement> PostAsync(string query)
    {
        var resp = await _client.PostAsJsonAsync("/graphql", new { query });
        var body = await resp.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static string IssueClinicianToken(string orgId)
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
                new(JwtRegisteredClaimNames.Email, "clinician@test.example"),
                new("org", orgId),
                new(ClaimTypes.Role, nameof(Role.Clinician)),
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
