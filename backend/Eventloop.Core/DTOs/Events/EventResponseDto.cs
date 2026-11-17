using Eventloop.Core.Enums;

namespace Eventloop.Core.DTOs.Events;

public record EventResponseDto(
    Guid Id,
    Guid OrganizerId,
    string OrganizerName,
    string Title,
    string Description,
    string Venue,
    DateTime StartTime,
    DateTime EndTime,
    int Capacity,
    EventStatus Status,
    DateTime CreatedAt,
    int ConfirmedCount,
    int WaitlistCount,
    int SpotsLeft,
    bool IsSoldOut
);
