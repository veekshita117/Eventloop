using Eventloop.Core.Entities;

namespace Eventloop.Core.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
