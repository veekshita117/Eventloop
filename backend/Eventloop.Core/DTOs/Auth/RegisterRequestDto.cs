namespace Eventloop.Core.DTOs.Auth;

public record RegisterRequestDto(
    string Name,
    string Email,
    string Password
);
