namespace SerenAuth.Application.Abstractions;

/// <summary>
/// PBKDF2-SHA256 password hashing contract. The concrete implementation
/// (and its iteration count / salt size) lives in Infrastructure.Security.
/// </summary>
public interface IPasswordHasher
{
    (string hash, string salt) Hash(string password);
    bool Verify(string password, string hash, string salt);
}
