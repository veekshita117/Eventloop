using System.Collections.Concurrent;
using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Registrations;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Eventloop.Infrastructure.Services;

public class RegistrationService : IRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IRegistrationEventPublisher _eventPublisher;

    // In-memory key-based semaphores to guarantee synchronization per EventId
    // across concurrent tasks, complementing SQL Server row-level locking.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> EventLocks = new();

    public RegistrationService(
        ApplicationDbContext context,
        IRegistrationEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    /*
     * CONCURRENCY PROTECTION EXPLANATION:
     * When multiple users compete for the final spots of an event, we must ensure
     * that we never overbook capacity (i.e. two people grabbing the last spot cannot both be confirmed).
     *
     * We achieve this through a 3-layer defense strategy:
     *
     * 1. Database Row-Level Locking (SQL Server in Production):
     *    When running on SQL Server, reading the Event row with 'WITH (UPDLOCK, ROWLOCK)'
     *    inside a database transaction acquires an exclusive update lock on the specific Event record.
     *    Any concurrent transaction reading that Event for registration is serialized and waits.
     *    Because the lock is held through the capacity calculation and registration insertion,
     *    the capacity evaluation (ConfirmedCount < Capacity) is atomic and race-free.
     *
     * 2. Keyed In-Memory Concurrency Semaphore:
     *    EF Core's in-memory provider does not execute raw SQL lock hints (WITH UPDLOCK).
     *    To provide guaranteed race-free execution during automated testing and in single-process
     *    environments, an async semaphore keyed by EventId guarantees that critical registration
     *    sections for a given event are strictly executed sequentially.
     *
     * 3. Database Schema Constraint (Defense in Depth):
     *    A UNIQUE constraint/index on (EventId, UserId) guarantees that even if a duplicate request
     *    bypasses the application layer, the database engine will reject duplicate registrations.
     */
    public async Task<RegistrationResponseDto> RegisterAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var semaphore = EventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Begin database transaction if supported by provider (SQL Server)
            var useDbTransaction = _context.Database.IsSqlServer();
            await using var transaction = useDbTransaction
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken)
                : null;

            // Load and lock the event row
            Event? eventEntity;
            if (useDbTransaction)
            {
                eventEntity = await _context.Events
                    .FromSqlRaw("SELECT * FROM Events WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", eventId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                eventEntity = await _context.Events
                    .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
            }

            if (eventEntity == null)
            {
                throw new NotFoundException("Event not found.");
            }

            if (eventEntity.Status != EventStatus.Published)
            {
                throw new InvalidOperationException($"Cannot register for an event that is not published. Current status: '{eventEntity.Status}'.");
            }

            if (eventEntity.StartTime <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot register for an event that has already started or ended.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            // Check if user is already registered for this event
            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken);

            if (existingRegistration != null)
            {
                if (existingRegistration.Status != RegistrationStatus.Cancelled)
                {
                    throw new ConflictException("You are already registered for this event.");
                }

                // If previously cancelled, remove old cancelled record so new registration can proceed with clean ticket
                _context.Registrations.Remove(existingRegistration);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Calculate current confirmed count
            var confirmedCount = await _context.Registrations
                .CountAsync(r => r.EventId == eventId &&
                    (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn), cancellationToken);

            Registration registration;

            if (confirmedCount < eventEntity.Capacity)
            {
                // Spot available -> Confirm
                registration = new Registration
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = userId,
                    Status = RegistrationStatus.Confirmed,
                    WaitlistPosition = null,
                    TicketCode = Guid.NewGuid(),
                    RegisteredAt = DateTime.UtcNow
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                await _eventPublisher.PublishTicketIssuedAsync(registration, eventEntity, cancellationToken);
            }
            else
            {
                // Event is full -> Place on waitlist
                var maxWaitlistPosition = await _context.Registrations
                    .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted)
                    .MaxAsync(r => (int?)r.WaitlistPosition, cancellationToken) ?? 0;

                registration = new Registration
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = userId,
                    Status = RegistrationStatus.Waitlisted,
                    WaitlistPosition = maxWaitlistPosition + 1,
                    TicketCode = Guid.NewGuid(),
                    RegisteredAt = DateTime.UtcNow
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }

            return MapToRegistrationResponseDto(registration, eventEntity, user);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<RegistrationResponseDto> CancelRegistrationAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var semaphore = EventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                throw new NotFoundException("Event not found.");
            }

            var registration = await _context.Registrations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId && r.Status != RegistrationStatus.Cancelled, cancellationToken);

            if (registration == null)
            {
                throw new NotFoundException("No active registration found for this event.");
            }

            if (registration.Status == RegistrationStatus.CheckedIn)
            {
                throw new InvalidOperationException("Cannot cancel a registration that has already checked in.");
            }

            var wasConfirmed = registration.Status == RegistrationStatus.Confirmed;

            // Mark registration as cancelled
            registration.Status = RegistrationStatus.Cancelled;
            registration.WaitlistPosition = null;
            await _context.SaveChangesAsync(cancellationToken);

            if (wasConfirmed)
            {
                // Automatically promote the first waitlisted user
                var topWaitlist = await _context.Registrations
                    .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted)
                    .OrderBy(r => r.WaitlistPosition)
                    .FirstOrDefaultAsync(cancellationToken);

                if (topWaitlist != null)
                {
                    topWaitlist.Status = RegistrationStatus.Confirmed;
                    topWaitlist.WaitlistPosition = null;
                    await _context.SaveChangesAsync(cancellationToken);

                    await _eventPublisher.PublishWaitlistPromotedAsync(topWaitlist, eventEntity, cancellationToken);
                }
            }

            // Renumber remaining waitlist positions sequentially
            await RenumberWaitlistPositionsAsync(eventId, cancellationToken);

            return MapToRegistrationResponseDto(registration, eventEntity, registration.User);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<int> PromoteWaitlistedAttendeesAsync(Guid eventId, int spotsAvailable, CancellationToken cancellationToken = default)
    {
        if (spotsAvailable <= 0)
        {
            return 0;
        }

        var semaphore = EventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return 0;
            }

            var waitlistedUsers = await _context.Registrations
                .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted)
                .OrderBy(r => r.WaitlistPosition)
                .Take(spotsAvailable)
                .ToListAsync(cancellationToken);

            if (waitlistedUsers.Count == 0)
            {
                return 0;
            }

            foreach (var reg in waitlistedUsers)
            {
                reg.Status = RegistrationStatus.Confirmed;
                reg.WaitlistPosition = null;
                await _eventPublisher.PublishWaitlistPromotedAsync(reg, eventEntity, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Renumber remaining waitlist
            await RenumberWaitlistPositionsAsync(eventId, cancellationToken);

            return waitlistedUsers.Count;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<MyRegistrationDto>> GetUserRegistrationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var registrations = await _context.Registrations
            .AsNoTracking()
            .Include(r => r.Event)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RegisteredAt)
            .Select(r => new MyRegistrationDto(
                r.Id,
                r.EventId,
                r.Event.Title,
                r.Event.Venue,
                r.Event.StartTime,
                r.Event.EndTime,
                r.Status,
                r.WaitlistPosition,
                r.TicketCode,
                r.RegisteredAt,
                r.CheckedInAt
            ))
            .ToListAsync(cancellationToken);

        return registrations;
    }

    public async Task<PagedResultDto<RegistrationResponseDto>> GetEventRegistrationsAsync(
        Guid eventId,
        Guid currentUserId,
        bool isAdmin,
        RegistrationFilterParametersDto filters,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        if (!isAdmin && eventEntity.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to view registrations for this event.");
        }

        var query = _context.Registrations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId);

        if (filters.Status.HasValue)
        {
            query = query.Where(r => r.Status == filters.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim().ToLower();
            query = query.Where(r => r.User.Name.ToLower().Contains(search) || r.User.Email.ToLower().Contains(search));
        }

        query = query
            .OrderBy(r => r.Status == RegistrationStatus.Confirmed ? 1 :
                          r.Status == RegistrationStatus.Waitlisted ? 2 :
                          r.Status == RegistrationStatus.CheckedIn ? 3 : 4)
            .ThenBy(r => r.WaitlistPosition)
            .ThenBy(r => r.RegisteredAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(r => MapToRegistrationResponseDto(r, r.Event, r.User))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RegistrationResponseDto>(items, totalCount, filters.Page, filters.PageSize);
    }

    private async Task RenumberWaitlistPositionsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var remainingWaitlist = await _context.Registrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.WaitlistPosition)
            .ThenBy(r => r.RegisteredAt)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < remainingWaitlist.Count; i++)
        {
            remainingWaitlist[i].WaitlistPosition = i + 1;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static RegistrationResponseDto MapToRegistrationResponseDto(Registration reg, Event eventEntity, User user)
    {
        return new RegistrationResponseDto(
            reg.Id,
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.StartTime,
            eventEntity.EndTime,
            eventEntity.Venue,
            user.Id,
            user.Name,
            user.Email,
            reg.Status,
            reg.WaitlistPosition,
            reg.TicketCode,
            reg.RegisteredAt,
            reg.CheckedInAt
        );
    }
}
