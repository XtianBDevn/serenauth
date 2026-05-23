using SerenAuth.Domain.Enums;
using SerenAuth.Domain.ValueObjects;

namespace SerenAuth.Domain.Entities;

/// <summary>
/// Core domain entity. Encapsulates the rules around the prior
/// authorization lifecycle so that the only way to mutate state is via
/// a domain method that enforces transition validity.
/// </summary>
public sealed class PriorAuthorization
{
    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public string OrganizationId { get; private set; } = string.Empty;
    public string PatientId { get; private set; } = string.Empty;
    public string ProviderId { get; private set; } = string.Empty;
    public string ProcedureCpt { get; private set; } = string.Empty;
    public string DiagnosisIcd10 { get; private set; } = string.Empty;
    public string Payer { get; private set; } = string.Empty;
    public PaStatus Status { get; private set; } = PaStatus.Draft;
    public double AiConfidence { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private PriorAuthorization() { }

    /// <summary>
    /// Factory that validates all inputs through their value objects.
    /// The status always starts as <see cref="PaStatus.Draft"/>.
    /// </summary>
    public static PriorAuthorization CreateDraft(
        string organizationId,
        string patientId,
        string providerId,
        CptCode cpt,
        Icd10Code icd10,
        Payer payer,
        double aiConfidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(cpt);
        ArgumentNullException.ThrowIfNull(icd10);
        ArgumentNullException.ThrowIfNull(payer);
        if (aiConfidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(aiConfidence), "AI confidence must be between 0 and 1.");
        }

        var now = DateTime.UtcNow;
        return new PriorAuthorization
        {
            OrganizationId = organizationId,
            PatientId = patientId,
            ProviderId = providerId,
            ProcedureCpt = cpt.Value,
            DiagnosisIcd10 = icd10.Value,
            Payer = payer.Name,
            Status = PaStatus.Draft,
            AiConfidence = aiConfidence,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Edits a draft PA's clinical fields. Allowed only while the PA is
    /// still in <see cref="PaStatus.Draft"/> — once submitted, the
    /// authorization is locked so the payer always sees what was
    /// approved on submission, not a later rewrite. Inputs flow through
    /// the same value objects as <see cref="CreateDraft"/> so the
    /// allowlist + range checks can't be skipped on the update path.
    /// </summary>
    public void Update(
        CptCode cpt,
        Icd10Code icd10,
        Payer payer,
        double aiConfidence)
    {
        if (Status != PaStatus.Draft)
        {
            throw new InvalidOperationException($"Only drafts can be edited (current status: {Status}).");
        }
        ArgumentNullException.ThrowIfNull(cpt);
        ArgumentNullException.ThrowIfNull(icd10);
        ArgumentNullException.ThrowIfNull(payer);
        if (aiConfidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(aiConfidence), "AI confidence must be between 0 and 1.");
        }

        ProcedureCpt = cpt.Value;
        DiagnosisIcd10 = icd10.Value;
        Payer = payer.Name;
        AiConfidence = aiConfidence;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transition Draft → Pending. Submission is irreversible.
    /// </summary>
    public void Submit()
    {
        if (Status != PaStatus.Draft)
        {
            throw new InvalidOperationException($"Only drafts can be submitted (current status: {Status}).");
        }
        Status = PaStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Payer-driven transition Pending → Approved.</summary>
    public void Approve()
    {
        if (Status != PaStatus.Pending)
        {
            throw new InvalidOperationException($"Only pending authorizations can be approved (current status: {Status}).");
        }
        Status = PaStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Payer-driven transition Pending → Denied.</summary>
    public void Deny()
    {
        if (Status != PaStatus.Pending)
        {
            throw new InvalidOperationException($"Only pending authorizations can be denied (current status: {Status}).");
        }
        Status = PaStatus.Denied;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Hydration constructor used by the persistence layer. Internal so
    /// callers outside Infrastructure cannot fabricate arbitrary state.
    /// </summary>
    internal static PriorAuthorization Rehydrate(
        string id,
        string organizationId,
        string patientId,
        string providerId,
        string procedureCpt,
        string diagnosisIcd10,
        string payer,
        PaStatus status,
        double aiConfidence,
        DateTime createdAt,
        DateTime updatedAt) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        PatientId = patientId,
        ProviderId = providerId,
        ProcedureCpt = procedureCpt,
        DiagnosisIcd10 = diagnosisIcd10,
        Payer = payer,
        Status = status,
        AiConfidence = aiConfidence,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
