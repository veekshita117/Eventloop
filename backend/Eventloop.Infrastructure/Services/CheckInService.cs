using System.Collections.Concurrent;
using System.Data;
using System.Text;
using Eventloop.Core.DTOs.CheckIn;
using Eventloop.Core.DTOs.Stats;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventloop.Infrastructure.Services;

public class CheckInService : ICheckInService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckInService> _logger;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> EventCheckInLocks = new();

    public CheckInService(
        ApplicationDbContext context,
        ILogger<CheckInService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TicketDetailsDto> GetTicketDetailsAsync(Guid registrationId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var registration = await _context.Registrations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);

        if (registration == null)
        {
            throw new NotFoundException("Registration not found.");
        }

        if (!isAdmin && registration.UserId != currentUserId && registration.Event.OrganizerId != currentUserId)
        {
            throw new ForbiddenException("You do not have permission to view this ticket.");
        }

        return new TicketDetailsDto(
            registration.Id,
            registration.TicketCode,
            registration.Event.Id,
            registration.Event.Title,
            registration.Event.Venue,
            registration.Event.StartTime,
            registration.Event.EndTime,
            registration.User.Name,
            registration.User.Email,
            registration.Status,
            registration.RegisteredAt,
            registration.CheckedInAt
        );
    }

    public async Task<CheckInResponseDto> CheckInTicketAsync(
        Guid eventId,
        Guid ticketCode,
        Guid scannerUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity == null)
        {
            throw new NotFoundException("Event not found.");
        }

        if (!isAdmin && eventEntity.OrganizerId != scannerUserId)
        {
            throw new ForbiddenException("You do not have permission to scan tickets for this event.");
        }

        // Lock to ensure idempotent check-in under concurrent/double scans
        var semaphore = EventCheckInLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var registration = await _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.TicketCode == ticketCode, cancellationToken);

            if (registration == null)
            {
                throw new NotFoundException("Ticket code not found.");
            }

            if (registration.EventId != eventId)
            {
                throw new InvalidOperationException($"This ticket is for a different event: '{registration.Event.Title}'.");
            }

            if (registration.Status == RegistrationStatus.CheckedIn)
            {
                throw new ConflictException(
                    $"Ticket was already checked in at {registration.CheckedInAt:yyyy-MM-dd HH:mm:ss} UTC by scanner {registration.CheckedInBy}.");
            }

            if (registration.Status == RegistrationStatus.Cancelled)
            {
                throw new InvalidOperationException("This registration was cancelled.");
            }

            if (registration.Status == RegistrationStatus.Waitlisted)
            {
                throw new InvalidOperationException(
                    $"This user is on the waitlist (Position #{registration.WaitlistPosition}) and does not have a confirmed ticket.");
            }

            // Mark checked-in
            registration.Status = RegistrationStatus.CheckedIn;
            registration.CheckedInAt = DateTime.UtcNow;
            registration.CheckedInBy = scannerUserId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Ticket {TicketCode} successfully checked in for {AttendeeName} at event '{EventTitle}' by scanner {ScannerId}",
                ticketCode,
                registration.User.Name,
                registration.Event.Title,
                scannerUserId);

            return new CheckInResponseDto(
                Success: true,
                Message: "Check-in successful.",
                registration.Id,
                registration.TicketCode,
                registration.User.Name,
                registration.User.Email,
                registration.Event.Title,
                registration.CheckedInAt.Value,
                scannerUserId
            );
        }
        finally
        {
            semaphore.Release();
        }
    }

    /*
     * HAND-WRITTEN SQL ATTENDANCE STATISTICS EXPLANATION:
     *
     * 1. 15-Minute Interval Aggregation:
     *    In SQL Server, we compute time buckets using integer math on minutes from the epoch:
     *    (DATEDIFF(minute, 0, CheckedInAt) / 15) * 15 truncates the timestamp to the lower 15-minute boundary.
     *    DATEADD(minute, ..., 0) converts the integer bucket back into a DATETIME value.
     *    GROUP BY on this expression groups check-ins into distinct 15-minute velocity intervals.
     *
     * 2. Daily Registration Counts:
     *    CAST(RegisteredAt AS DATE) strips the time component, allowing COUNT(*) grouped by day.
     *
     * 3. Provider-Aware Execution:
     *    When executing on SQL Server, raw SQL is dispatched directly over the ADO.NET connection.
     *    For non-relational test providers (like EF Core InMemory), an equivalent LINQ aggregation
     *    is executed so tests run fast, reliably, and without external SQL Server dependencies.
     */
    public async Task<EventAttendanceStatsDto> GetAttendanceStatsAsync(
        Guid eventId,
        Guid currentUserId,
        bool isAdmin,
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
            throw new ForbiddenException("You do not have permission to view statistics for this event.");
        }

        var confirmedCount = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed, cancellationToken);

        var checkedInCount = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.CheckedIn, cancellationToken);

        var waitlistCount = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted, cancellationToken);

        var cancelledCount = await _context.Registrations
            .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Cancelled, cancellationToken);

        var totalExpectedAttendees = confirmedCount + checkedInCount;
        var noShowRate = totalExpectedAttendees == 0
            ? 0.0
            : Math.Round((double)confirmedCount / totalExpectedAttendees * 100, 2);

        List<CheckInIntervalStatDto> intervals;
        List<DailyRegistrationStatDto> dailyStats;

        if (_context.Database.IsSqlServer())
        {
            intervals = await ExecuteSqlIntervalsAsync(eventId, cancellationToken);
            dailyStats = await ExecuteSqlDailyRegistrationsAsync(eventId, cancellationToken);
        }
        else
        {
            // InMemory / Test fallback
            intervals = await ExecuteInMemoryIntervalsAsync(eventId, cancellationToken);
            dailyStats = await ExecuteInMemoryDailyRegistrationsAsync(eventId, cancellationToken);
        }

        return new EventAttendanceStatsDto(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Capacity,
            confirmedCount,
            checkedInCount,
            waitlistCount,
            cancelledCount,
            noShowRate,
            intervals,
            dailyStats
        );
    }

    public async Task<byte[]> ExportAttendeesCsvAsync(
        Guid eventId,
        Guid currentUserId,
        bool isAdmin,
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
            throw new ForbiddenException("You do not have permission to export attendees for this event.");
        }

        var registrations = await _context.Registrations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.CheckedInByUser)
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.Status == RegistrationStatus.CheckedIn ? 1 :
                          r.Status == RegistrationStatus.Confirmed ? 2 :
                          r.Status == RegistrationStatus.Waitlisted ? 3 : 4)
            .ThenBy(r => r.WaitlistPosition)
            .ThenBy(r => r.RegisteredAt)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("TicketCode,Name,Email,Status,WaitlistPosition,RegisteredAtUtc,CheckedInAtUtc,CheckedInBy");

        foreach (var reg in registrations)
        {
            var line = string.Join(",",
                EscapeCsv(reg.TicketCode.ToString()),
                EscapeCsv(reg.User.Name),
                EscapeCsv(reg.User.Email),
                EscapeCsv(reg.Status.ToString()),
                EscapeCsv(reg.WaitlistPosition?.ToString() ?? ""),
                EscapeCsv(reg.RegisteredAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(reg.CheckedInAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""),
                EscapeCsv(reg.CheckedInByUser?.Name ?? reg.CheckedInBy?.ToString() ?? "")
            );
            sb.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private async Task<List<CheckInIntervalStatDto>> ExecuteSqlIntervalsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var results = new List<CheckInIntervalStatDto>();
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                DATEADD(minute, (DATEDIFF(minute, 0, CheckedInAt) / 15) * 15, 0) AS IntervalStart,
                COUNT(*) AS [Count]
            FROM Registrations
            WHERE EventId = @EventId AND Status = 'CheckedIn' AND CheckedInAt IS NOT NULL
            GROUP BY DATEADD(minute, (DATEDIFF(minute, 0, CheckedInAt) / 15) * 15, 0)
            ORDER BY IntervalStart ASC;
        ";

        var param = command.CreateParameter();
        param.ParameterName = "@EventId";
        param.Value = eventId;
        command.Parameters.Add(param);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var start = reader.GetDateTime(0);
            var count = reader.GetInt32(1);
            results.Add(new CheckInIntervalStatDto(start, count));
        }

        return results;
    }

    private async Task<List<DailyRegistrationStatDto>> ExecuteSqlDailyRegistrationsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var results = new List<DailyRegistrationStatDto>();
        var connection = _context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                CAST(RegisteredAt AS DATE) AS [Date],
                COUNT(*) AS [Count]
            FROM Registrations
            WHERE EventId = @EventId
            GROUP BY CAST(RegisteredAt AS DATE)
            ORDER BY [Date] ASC;
        ";

        var param = command.CreateParameter();
        param.ParameterName = "@EventId";
        param.Value = eventId;
        command.Parameters.Add(param);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = reader.GetDateTime(0);
            var count = reader.GetInt32(1);
            results.Add(new DailyRegistrationStatDto(date, count));
        }

        return results;
    }

    private async Task<List<CheckInIntervalStatDto>> ExecuteInMemoryIntervalsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var checkInTimes = await _context.Registrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.CheckedIn && r.CheckedInAt != null)
            .Select(r => r.CheckedInAt!.Value)
            .ToListAsync(cancellationToken);

        return checkInTimes
            .GroupBy(t => new DateTime(t.Year, t.Month, t.Day, t.Hour, (t.Minute / 15) * 15, 0, DateTimeKind.Utc))
            .OrderBy(g => g.Key)
            .Select(g => new CheckInIntervalStatDto(g.Key, g.Count()))
            .ToList();
    }

    private async Task<List<DailyRegistrationStatDto>> ExecuteInMemoryDailyRegistrationsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var regTimes = await _context.Registrations
            .Where(r => r.EventId == eventId)
            .Select(r => r.RegisteredAt)
            .ToListAsync(cancellationToken);

        return regTimes
            .GroupBy(t => t.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyRegistrationStatDto(g.Key, g.Count()))
            .ToList();
    }
}
