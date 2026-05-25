using MediatR;

namespace SerenAuth.Application.Auth;

/// <summary>
/// Self-service password rotation. The caller's identity comes from the
/// validated JWT (<c>ICurrentUser.UserId</c>) — never from the request
/// body — so a token-holder can only ever change their own password.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword)
    : IRequest<ChangePasswordResultDto>;

public sealed record ChangePasswordResultDto(string Email, DateTime ChangedAt);
