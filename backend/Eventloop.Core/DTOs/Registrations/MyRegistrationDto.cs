using Eventloop.Core.Enums;

namespace Eventloop.Core.DTOs.Registrations;

public record MyRegistrationDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    string EventVenue,
    DateTime EventStartTime,
    DateTime EventEndTime,
    RegistrationStatus Status,
    int? WaitlistPosition,
    Guid TicketCode,
    DateTime RegisteredAt,
    DateTime? CheckedInAt
);
