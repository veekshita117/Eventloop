namespace Eventloop.Core.DTOs.Events;

public record CreateEventRequestDto(
    string Title,
    string Description,
    string Venue,
    DateTime StartTime,
    DateTime EndTime,
    int Capacity
);
