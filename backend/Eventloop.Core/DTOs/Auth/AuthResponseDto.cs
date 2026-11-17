using Eventloop.Core.DTOs.User;

namespace Eventloop.Core.DTOs.Auth;

public record AuthResponseDto(
    string Token,
    UserDto User
);
