using MediatR;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Abstractions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Application.Auth;

/// <summary>
/// Verifies the caller's current password (constant-time) and replaces
/// the credential pair with a fresh PBKDF2 hash + salt. Failures throw
/// <see cref="UnauthorizedAccessException"/> so the GraphQL error filter
/// emits a single redacted message — we never leak whether the user id
/// was found vs. the password was wrong.
/// </summary>
public sealed class ChangePasswordHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    ICurrentUser currentUser,
    IAuditEventRepository audit)
    : IRequestHandler<ChangePasswordCommand, ChangePasswordResultDto>
{
    public async Task<ChangePasswordResultDto> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var (hash, salt) = hasher.Hash(request.NewPassword);
        user.ChangePassword(hash, salt);

        await users.UpdateAsync(user, cancellationToken);

        await audit.InsertAsync(new AuditEvent
        {
            Action = AuditAction.CHANGE_PASSWORD,
            Entity = nameof(User),
            EntityId = user.Id,
            UserId = user.Id,
            OrganizationId = user.OrganizationId,
        }, cancellationToken);

        return new ChangePasswordResultDto(user.Email, user.PasswordChangedAt);
    }
}
