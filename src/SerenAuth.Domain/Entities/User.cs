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

    // Hash + salt are mutable through ChangePassword only — direct
    // assignment from outside the domain is disallowed so we can't end
    // up with a hash/salt pair that wasn't computed together.
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime PasswordChangedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Replaces the credential pair atomically. Both inputs come from
    /// <c>IPasswordHasher.Hash</c> and must be a matched pair; callers
    /// should never combine a hash from one call with a salt from another.
    /// </summary>
    public void ChangePassword(string hash, string salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);
        PasswordHash = hash;
        PasswordSalt = salt;
        PasswordChangedAt = DateTime.UtcNow;
    }

}
