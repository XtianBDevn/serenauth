namespace SerenAuth.Domain.Entities;

/// <summary>
/// HIPAA: minimum necessary PHI. We persist only the fields required to
/// adjudicate a prior auth. No SSN, no address, no demographics beyond
/// what payers require for medical-necessity review.
/// </summary>
public sealed class Patient
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string OrganizationId { get; init; } = string.Empty;
    public string ExternalMrn { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
