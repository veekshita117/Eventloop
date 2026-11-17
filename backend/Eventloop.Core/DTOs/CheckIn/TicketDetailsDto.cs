using Eventloop.Core.Enums;

namespace Eventloop.Core.DTOs.CheckIn;

public record TicketDetailsDto(
    Guid RegistrationId,
    Guid TicketCode,
    Guid EventId,
    string EventTitle,
    string EventVenue,
    DateTime EventStartTime,
    DateTime EventEndTime,
    string AttendeeName,
    string AttendeeEmail,
    RegistrationStatus Status,
    DateTime RegisteredAt,
    DateTime? CheckedInAt
);
