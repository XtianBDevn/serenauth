namespace SerenAuth.Domain.Entities;

/// <summary>
/// A clinician (e.g. nephrologist) responsible for ordering dialysis.
/// NPI is stored verbatim — validation happens at the application layer.
/// </summary>
public sealed class Provider
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string OrganizationId { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Npi { get; init; } = string.Empty;
    public string Specialty { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
