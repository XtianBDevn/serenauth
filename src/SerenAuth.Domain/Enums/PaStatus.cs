namespace SerenAuth.Domain.Enums;

/// <summary>
/// Lifecycle of a prior authorization. Transitions are enforced in
/// <see cref="SerenAuth.Domain.Entities.PriorAuthorization"/>.
/// </summary>
public enum PaStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Denied = 3,

    /// <summary>
    /// Clinician pulled the PA back before the payer responded. Terminal —
    /// like Approved/Denied — but distinguishes "we took it back" from
    /// "payer said no" so the audit story stays honest.
    /// </summary>
    Withdrawn = 4
}
