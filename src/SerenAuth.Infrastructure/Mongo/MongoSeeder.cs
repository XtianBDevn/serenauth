using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;
using SerenAuth.Domain.ValueObjects;
using SerenAuth.Infrastructure.Options;

namespace SerenAuth.Infrastructure.Mongo;

/// <summary>
/// Idempotently seeds the demo dataset described in the product brief:
/// 1 org, 3 users, 2 providers, 5 patients, 10 prior authorizations.
/// Controlled by <c>Seeding:Enabled</c>; never run with real PHI.
/// </summary>
public sealed class MongoSeeder(
    MongoContext ctx,
    IPasswordHasher hasher,
    IOptions<SeedingOptions> opts,
    ILogger<MongoSeeder> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!opts.Value.Enabled)
        {
            return;
        }

        if (await ctx.Organizations.Find(_ => true).AnyAsync(cancellationToken))
        {
            log.LogInformation("Seed skipped: data already present.");
            return;
        }

        log.LogInformation("Seeding demo data for SerenAuth.");

        var org = new Organization { Name = "Riverbend Dialysis Center" };
        await ctx.Organizations.InsertOneAsync(org, cancellationToken: cancellationToken);

        var users = new[]
        {
            BuildUser(hasher, org.Id, "admin@riverbend.example",  "Avery Carter",  Role.Admin),
            BuildUser(hasher, org.Id, "clin@riverbend.example",   "Dr. Mira Patel", Role.Clinician),
            BuildUser(hasher, org.Id, "intake@riverbend.example", "Jordan Ellis",  Role.Intake),
        };
        await ctx.Users.InsertManyAsync(users, cancellationToken: cancellationToken);

        var providers = new[]
        {
            new Provider { OrganizationId = org.Id, FirstName = "Mira",   LastName = "Patel",  Npi = "1234567890", Specialty = "Nephrology" },
            new Provider { OrganizationId = org.Id, FirstName = "Ethan",  LastName = "Brooks", Npi = "9876543210", Specialty = "Nephrology" },
        };
        await ctx.Providers.InsertManyAsync(providers, cancellationToken: cancellationToken);

        var patients = Enumerable.Range(1, 5).Select(i => new Patient
        {
            OrganizationId = org.Id,
            ExternalMrn = $"MRN-{1000 + i}",
            FirstName = $"Patient{i}",
            LastName = $"Demo{i}",
            DateOfBirth = new DateOnly(1955 + i, 1, 1)
        }).ToArray();
        await ctx.Patients.InsertManyAsync(patients, cancellationToken: cancellationToken);

        var payers = new[] { "BCBS", "Aetna", "United Healthcare", "Humana", "Medicare" };
        var statuses = new[] { PaStatus.Draft, PaStatus.Pending, PaStatus.Approved, PaStatus.Denied };
        var cpts = new[] { "90935", "90937" };
        var pas = new List<PriorAuthorization>();
        for (var i = 0; i < 10; i++)
        {
            var pat = patients[i % patients.Length];
            var prov = providers[i % providers.Length];
            var pa = PriorAuthorization.CreateDraft(
                org.Id, pat.Id, prov.Id,
                CptCode.Create(cpts[i % cpts.Length]),
                Icd10Code.Create("N18.6"),
                Payer.Create(payers[i % payers.Length]),
                aiConfidence: Math.Round(0.70 + ((i * 0.027) % 0.30), 2));

            // Stagger statuses for a realistic dashboard view.
            var target = statuses[i % statuses.Length];
            if (target is PaStatus.Pending or PaStatus.Approved or PaStatus.Denied)
            {
                pa.Submit();
                if (target == PaStatus.Approved) pa.Approve();
                else if (target == PaStatus.Denied) pa.Deny();
            }
            pas.Add(pa);
        }
        await ctx.PriorAuthorizations.InsertManyAsync(pas, cancellationToken: cancellationToken);

        log.LogInformation("Seed complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static User BuildUser(IPasswordHasher hasher, string orgId, string email, string name, Role role)
    {
        var (hash, salt) = hasher.Hash("ChangeMe!123");
        return new User
        {
            OrganizationId = orgId,
            Email = email,
            DisplayName = name,
            Role = role,
            PasswordHash = hash,
            PasswordSalt = salt
        };
    }
}
