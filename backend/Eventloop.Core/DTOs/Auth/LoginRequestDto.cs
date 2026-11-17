namespace Eventloop.Core.DTOs.Auth;

public record LoginRequestDto(
    string Email,
    string Password
);
