using SerenAuth.Domain.Entities;

namespace SerenAuth.Application.Abstractions;

public interface IJwtTokenService
{
    string Issue(User user);
}
