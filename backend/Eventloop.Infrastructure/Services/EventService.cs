using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Events;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eventloop.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly IRegistrationService _registrationService;

    public EventService(ApplicationDbContext context, IRegistrationService registrationService)
    {
        _context = context;
        _registrationService = registrationService;
    }

    public async Task<EventResponseDto> CreateEventAsync(CreateEventRequestDto request, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var organizer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == organizerId, cancellationToken);

        if (organizer == null)
        {
            throw new NotFoundException("Organizer user not found.");
        }

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Venue = request.Venue.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            Status = EventStatus.Draft, // Starts as Draft
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToEventResponseDto(eventEntity, organizer.Name, confirmedCount: 0, waitlistCount: 0);
    }

    public async Task<EventResponseDto> UpdateEventAsync(
        Guid eventId,
        UpdateEventRequestDto request,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        ValidateOwnership(eventEntity, currentUserId, isAdmin);

        if (eventEntity.Status == EventStatus.Completed || eventEntity.Status == EventStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot update an event with status '{eventEntity.Status}'.");
        }

        var confirmedCount = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn), cancellationToken);

        var waitlistCount = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted, cancellationToken);

        if (request.Capacity < confirmedCount)
        {
            throw new InvalidOperationException($"Capacity cannot be reduced below the number of confirmed registrations ({confirmedCount}).");
        }

        var previousCapacity = eventEntity.Capacity;

        eventEntity.Title = request.Title.Trim();
        eventEntity.Description = request.Description.Trim();
        eventEntity.Venue = request.Venue.Trim();
        eventEntity.StartTime = request.StartTime;
        eventEntity.EndTime = request.EndTime;
        eventEntity.Capacity = request.Capacity;

        await _context.SaveChangesAsync(cancellationToken);

        // When capacity is increased, promote waitlisted users for the newly available spots
        if (request.Capacity > previousCapacity && waitlistCount > 0)
        {
            var spotsOpened = request.Capacity - previousCapacity;
            await _registrationService.PromoteWaitlistedAttendeesAsync(eventId, spotsOpened, cancellationToken);
        }

        return MapToEventResponseDto(eventEntity, eventEntity.Organizer.Name, confirmedCount, waitlistCount);
    }

    public async Task DeleteEventAsync(
        Guid eventId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        ValidateOwnership(eventEntity, currentUserId, isAdmin);

        if (eventEntity.Status != EventStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft events can be deleted. Current event status is '{eventEntity.Status}'.");
        }

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EventResponseDto> PublishEventAsync(
        Guid eventId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        ValidateOwnership(eventEntity, currentUserId, isAdmin);

        if (eventEntity.Status != EventStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft events can be published. Current status is '{eventEntity.Status}'.");
        }

        eventEntity.Status = EventStatus.Published;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToEventResponseDto(eventEntity, eventEntity.Organizer.Name, confirmedCount: 0, waitlistCount: 0);
    }

    public async Task<EventResponseDto> CancelEventAsync(
        Guid eventId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        ValidateOwnership(eventEntity, currentUserId, isAdmin);

        if (eventEntity.Status == EventStatus.Cancelled)
        {
            throw new InvalidOperationException("Event is already cancelled.");
        }

        if (eventEntity.Status == EventStatus.Completed)
        {
            throw new InvalidOperationException("Completed events cannot be cancelled.");
        }

        eventEntity.Status = EventStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        var counts = await GetEventRegistrationCountsAsync(eventId, cancellationToken);

        return MapToEventResponseDto(eventEntity, eventEntity.Organizer.Name, counts.Confirmed, counts.Waitlist);
    }

    public async Task<EventResponseDto> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        var counts = await GetEventRegistrationCountsAsync(eventId, cancellationToken);

        return MapToEventResponseDto(eventEntity, eventEntity.Organizer.Name, counts.Confirmed, counts.Waitlist);
    }

    public async Task<PagedResultDto<EventResponseDto>> GetPublishedEventsAsync(
        EventQueryParametersDto query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published);

        // Search by title
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            queryable = queryable.Where(e => e.Title.ToLower().Contains(search));
        }

        // Date range filters
        if (query.From.HasValue)
        {
            queryable = queryable.Where(e => e.StartTime >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            queryable = queryable.Where(e => e.StartTime <= query.To.Value);
        }

        // Has spots filter
        if (query.HasSpots.HasValue && query.HasSpots.Value)
        {
            queryable = queryable.Where(e => e.Capacity > e.Registrations
                .Count(r => r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn));
        }

        // Sorting
        queryable = query.SortBy?.ToLowerInvariant() switch
        {
            "title" => query.SortDescending
                ? queryable.OrderByDescending(e => e.Title)
                : queryable.OrderBy(e => e.Title),
            "createdat" => query.SortDescending
                ? queryable.OrderByDescending(e => e.CreatedAt)
                : queryable.OrderBy(e => e.CreatedAt),
            "capacity" => query.SortDescending
                ? queryable.OrderByDescending(e => e.Capacity)
                : queryable.OrderBy(e => e.Capacity),
            _ => query.SortDescending
                ? queryable.OrderByDescending(e => e.StartTime)
                : queryable.OrderBy(e => e.StartTime)
        };

        var totalCount = await queryable.CountAsync(cancellationToken);

        var events = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new
            {
                Event = e,
                OrganizerName = e.Organizer.Name,
                ConfirmedCount = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn),
                WaitlistCount = e.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted)
            })
            .ToListAsync(cancellationToken);

        var items = events.Select(x => MapToEventResponseDto(x.Event, x.OrganizerName, x.ConfirmedCount, x.WaitlistCount)).ToList();

        return new PagedResultDto<EventResponseDto>(items, totalCount, query.Page, query.PageSize);
    }

    private static void ValidateOwnership(Event eventEntity, Guid currentUserId, bool isAdmin)
    {
        if (!isAdmin && eventEntity.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to manage this event.");
        }
    }

    private async Task<(int Confirmed, int Waitlist)> GetEventRegistrationCountsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var confirmed = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn), cancellationToken);

        var waitlist = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted, cancellationToken);

        return (confirmed, waitlist);
    }

    private static EventResponseDto MapToEventResponseDto(Event eventEntity, string organizerName, int confirmedCount, int waitlistCount)
    {
        var spotsLeft = Math.Max(0, eventEntity.Capacity - confirmedCount);
        var isSoldOut = spotsLeft == 0;

        return new EventResponseDto(
            eventEntity.Id,
            eventEntity.OrganizerId,
            organizerName,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Venue,
            eventEntity.StartTime,
            eventEntity.EndTime,
            eventEntity.Capacity,
            eventEntity.Status,
            eventEntity.CreatedAt,
            confirmedCount,
            waitlistCount,
            spotsLeft,
            isSoldOut
        );
    }
}
