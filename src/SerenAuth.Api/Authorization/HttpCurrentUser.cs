using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Enums;

namespace SerenAuth.Api.Authorization;

/// <summary>
/// Adapter that exposes the validated JWT claims (and per-request IP /
/// correlation ID) to the application layer.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext? Ctx => accessor.HttpContext;

    public string UserId => Ctx?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
    public string OrganizationId => Ctx?.User.FindFirst("org")?.Value ?? string.Empty;
    public Role Role => Enum.TryParse<Role>(Ctx?.User.FindFirst(ClaimTypes.Role)?.Value, out var r) ? r : Role.Viewer;
    public string IpAddress => Ctx?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    public string CorrelationId =>
        Ctx?.Items.TryGetValue("CorrelationId", out var v) == true
            ? v?.ToString() ?? string.Empty
            : string.Empty;
    public bool IsAuthenticated => Ctx?.User.Identity?.IsAuthenticated == true;
}
