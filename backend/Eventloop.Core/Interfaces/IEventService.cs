using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Events;

namespace Eventloop.Core.Interfaces;

public interface IEventService
{
    Task<EventResponseDto> CreateEventAsync(CreateEventRequestDto request, Guid organizerId, CancellationToken cancellationToken = default);
    Task<EventResponseDto> UpdateEventAsync(Guid eventId, UpdateEventRequestDto request, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid eventId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<EventResponseDto> PublishEventAsync(Guid eventId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<EventResponseDto> CancelEventAsync(Guid eventId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<PagedResultDto<EventResponseDto>> GetPublishedEventsAsync(EventQueryParametersDto query, CancellationToken cancellationToken = default);
    Task<EventResponseDto> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventResponseDto>> GetMyEventsAsync(Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
