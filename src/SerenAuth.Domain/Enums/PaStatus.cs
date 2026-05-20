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
    Denied = 3
}
