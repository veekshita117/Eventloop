using Eventloop.Core.Enums;

namespace Eventloop.Core.DTOs.User;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    DateTime CreatedAt
);
