using SerenAuth.Domain.Enums;

namespace SerenAuth.Domain.Entities;

/// <summary>
/// An end-user of SerenAuth. Passwords are never stored in plaintext;
/// only a PBKDF2 hash + salt is persisted via the Infrastructure layer.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string OrganizationId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public Role Role { get; init; } = Role.Viewer;
    public string PasswordHash { get; init; } = string.Empty;
    public string PasswordSalt { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
