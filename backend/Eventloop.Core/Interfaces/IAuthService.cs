using Eventloop.Core.DTOs.Auth;
using Eventloop.Core.DTOs.User;

namespace Eventloop.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto> PromoteToOrganizerAsync(Guid userId, CancellationToken cancellationToken = default);
}
