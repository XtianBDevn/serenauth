using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.Dtos;

public sealed record PriorAuthorizationDto(
    string Id,
    string PatientId,
    string ProviderId,
    string ProcedureCpt,
    string DiagnosisIcd10,
    string Payer,
    PaStatus Status,
    double AiConfidence,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static PriorAuthorizationDto FromEntity(PriorAuthorization pa) => new(
        pa.Id,
        pa.PatientId,
        pa.ProviderId,
        pa.ProcedureCpt,
        pa.DiagnosisIcd10,
        pa.Payer,
        pa.Status,
        pa.AiConfidence,
        pa.CreatedAt,
        pa.UpdatedAt);
}

public sealed record PatientDto(
    string Id,
    string ExternalMrn,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth)
{
    public static PatientDto FromEntity(Patient p) =>
        new(p.Id, p.ExternalMrn, p.FirstName, p.LastName, p.DateOfBirth);
}

public sealed record ProviderDto(
    string Id,
    string FirstName,
    string LastName,
    string Npi,
    string Specialty)
{
    public static ProviderDto FromEntity(Provider p) =>
        new(p.Id, p.FirstName, p.LastName, p.Npi, p.Specialty);
}

public sealed record LoginResultDto(
    string Token,
    string Email,
    string DisplayName,
    Role Role,
    string OrganizationId,
    DateTime IssuedAt);
