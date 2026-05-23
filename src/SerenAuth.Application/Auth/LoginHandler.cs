using MediatR;
using SerenAuth.Application.Abstractions;
using SerenAuth.Application.Dtos;
using SerenAuth.Domain.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.Auth;

/// <summary>
/// Validates credentials with a constant-time hash compare and, on success,
/// issues a short-lived JWT. Failures throw <see cref="UnauthorizedAccessException"/>
/// so the GraphQL error filter can surface a single, redacted message —
/// we never leak "wrong password" vs "no such user".
/// </summary>
public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    IAuditEventRepository audit)
    : IRequestHandler<LoginCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(normalized, cancellationToken);

        // Constant-ish-time response: verify against a throwaway hash when the
        // user is missing so an attacker can't time-side-channel valid emails.
        if (user is null)
        {
            _ = hasher.Verify(request.Password, DecoyHash, DecoySalt);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var token = tokens.Issue(user);

        await audit.InsertAsync(new AuditEvent
        {
            Action = AuditAction.LOGIN,
            Entity = nameof(User),
            EntityId = user.Id,
            UserId = user.Id,
            OrganizationId = user.OrganizationId,
        }, cancellationToken);

        return new LoginResultDto(
            Token: token,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Role: user.Role,
            OrganizationId: user.OrganizationId,
            IssuedAt: DateTime.UtcNow);
    }

    // Pre-computed decoy so failed lookups still spend roughly equal time on
    // a PBKDF2 verification. The exact values are unimportant.
    private const string DecoyHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string DecoySalt = "AAAAAAAAAAAAAAAAAAAAAA==";
}
