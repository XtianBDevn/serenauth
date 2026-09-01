using MediatR;
using SerenAuth.Application.Dtos;

namespace SerenAuth.Application.Auth;

/// <summary>
/// Exchanges email + password for a short-lived JWT. The handler is
/// the only place outside the Infrastructure layer that touches the
/// password hash + salt, and it always uses a constant-time compare.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResultDto>;
