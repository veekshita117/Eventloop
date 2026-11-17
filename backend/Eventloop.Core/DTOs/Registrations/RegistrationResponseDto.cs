using Eventloop.Core.Enums;

namespace Eventloop.Core.DTOs.Registrations;

public record RegistrationResponseDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    DateTime EventStartTime,
    DateTime EventEndTime,
    string EventVenue,
    Guid UserId,
    string UserName,
    string UserEmail,
    RegistrationStatus Status,
    int? WaitlistPosition,
    Guid TicketCode,
    DateTime RegisteredAt,
    DateTime? CheckedInAt
);
