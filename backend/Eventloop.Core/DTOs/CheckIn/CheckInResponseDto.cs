namespace Eventloop.Core.DTOs.CheckIn;

public record CheckInResponseDto(
    bool Success,
    string Message,
    Guid RegistrationId,
    Guid TicketCode,
    string AttendeeName,
    string AttendeeEmail,
    string EventTitle,
    DateTime CheckedInAt,
    Guid CheckedInBy
);
