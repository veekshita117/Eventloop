using System.Text;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Eventloop.Tests;

public class CheckInServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly CheckInService _service;
    private readonly User _organizer;
    private readonly User _student;
    private readonly Event _event;

    public CheckInServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _service = new CheckInService(_context, NullLogger<CheckInService>.Instance);

        _organizer = new User
        {
            Id = Guid.NewGuid(),
            Name = "Club Lead",
            Email = "lead@college.edu",
            PasswordHash = "hash",
            Role = UserRole.Organizer
        };

        _student = new User
        {
            Id = Guid.NewGuid(),
            Name = "Alice Student",
            Email = "alice@college.edu",
            PasswordHash = "hash",
            Role = UserRole.Student
        };

        _event = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = _organizer.Id,
            Title = "Spring Hackathon",
            Description = "Annual college hackathon",
            Venue = "Main Auditorium",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(6),
            Capacity = 50,
            Status = EventStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.Users.AddRange(_organizer, _student);
        _context.Events.Add(_event);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CheckInTicketAsync_ValidConfirmedTicket_MarksCheckedInAndReturnsSuccess()
    {
        // Arrange
        var ticketCode = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CheckInTicketAsync(_event.Id, ticketCode, _organizer.Id, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TicketCode.Should().Be(ticketCode);
        result.AttendeeName.Should().Be(_student.Name);
        result.AttendeeEmail.Should().Be(_student.Email);
        result.CheckedInBy.Should().Be(_organizer.Id);

        var dbReg = await _context.Registrations.FindAsync(registration.Id);
        dbReg!.Status.Should().Be(RegistrationStatus.CheckedIn);
        dbReg.CheckedInAt.Should().NotBeNull();
        dbReg.CheckedInBy.Should().Be(_organizer.Id);
    }

    [Fact]
    public async Task CheckInTicketAsync_AlreadyCheckedIn_ThrowsConflictException()
    {
        // Arrange
        var ticketCode = Guid.NewGuid();
        var checkedInTime = DateTime.UtcNow.AddMinutes(-30);
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-2),
            CheckedInAt = checkedInTime,
            CheckedInBy = _organizer.Id
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CheckInTicketAsync(_event.Id, ticketCode, _organizer.Id, isAdmin: false);

        // Assert
        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.WithMessage("*already checked in*");
    }

    [Fact]
    public async Task CheckInTicketAsync_WrongEvent_ThrowsInvalidOperationException()
    {
        // Arrange
        var anotherEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = _organizer.Id,
            Title = "Another Event",
            Description = "Different event",
            Venue = "Room B",
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            Capacity = 20,
            Status = EventStatus.Published
        };
        _context.Events.Add(anotherEvent);

        var ticketCode = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = anotherEvent.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act - scanning ticket for _event.Id instead of anotherEvent.Id
        var act = () => _service.CheckInTicketAsync(_event.Id, ticketCode, _organizer.Id, isAdmin: false);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*different event*");
    }

    [Fact]
    public async Task CheckInTicketAsync_CancelledTicket_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketCode = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Cancelled,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CheckInTicketAsync(_event.Id, ticketCode, _organizer.Id, isAdmin: false);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*cancelled*");
    }

    [Fact]
    public async Task CheckInTicketAsync_WaitlistedTicket_ThrowsInvalidOperationException()
    {
        // Arrange
        var ticketCode = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Waitlisted,
            WaitlistPosition = 1,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CheckInTicketAsync(_event.Id, ticketCode, _organizer.Id, isAdmin: false);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*waitlist*");
    }

    [Fact]
    public async Task CheckInTicketAsync_TicketNotFound_ThrowsNotFoundException()
    {
        // Act
        var act = () => _service.CheckInTicketAsync(_event.Id, Guid.NewGuid(), _organizer.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CheckInTicketAsync_EventNotFound_ThrowsNotFoundException()
    {
        // Act
        var act = () => _service.CheckInTicketAsync(Guid.NewGuid(), Guid.NewGuid(), _organizer.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CheckInTicketAsync_NonOrganizerNonAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var strangerId = Guid.NewGuid();

        // Act
        var act = () => _service.CheckInTicketAsync(_event.Id, Guid.NewGuid(), strangerId, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CheckInTicketAsync_AdminScanner_CanCheckInOtherOrganizersEvent()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var ticketCode = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CheckInTicketAsync(_event.Id, ticketCode, adminId, isAdmin: true);

        // Assert
        result.Success.Should().BeTrue();
        result.CheckedInBy.Should().Be(adminId);
    }

    [Fact]
    public async Task GetTicketDetailsAsync_Owner_ReturnsTicketDetails()
    {
        // Arrange
        var ticketCode = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = ticketCode,
            RegisteredAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTicketDetailsAsync(registration.Id, _student.Id, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result.TicketCode.Should().Be(ticketCode);
        result.AttendeeName.Should().Be(_student.Name);
        result.EventTitle.Should().Be(_event.Title);
        result.Status.Should().Be(RegistrationStatus.Confirmed);
    }

    [Fact]
    public async Task GetTicketDetailsAsync_Organizer_ReturnsTicketDetails()
    {
        // Arrange
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTicketDetailsAsync(registration.Id, _organizer.Id, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be(_event.Id);
    }

    [Fact]
    public async Task GetTicketDetailsAsync_UnauthorizedUser_ThrowsForbiddenException()
    {
        // Arrange
        var otherStudentId = Guid.NewGuid();
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.GetTicketDetailsAsync(registration.Id, otherStudentId, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetTicketDetailsAsync_NotFound_ThrowsNotFoundException()
    {
        // Act
        var act = () => _service.GetTicketDetailsAsync(Guid.NewGuid(), _student.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAttendanceStatsAsync_CalculatesCorrectCountsAndNoShowRate()
    {
        // Arrange:
        // 2 Confirmed, 3 CheckedIn, 2 Waitlisted, 1 Cancelled
        // Total expected = 2 + 3 = 5
        // Confirmed = 2
        // CheckedIn = 3
        // No-show rate = (2 / 5) * 100 = 40.0%
        var baseDate = DateTime.UtcNow.Date;

        var users = Enumerable.Range(1, 8).Select(i => new User
        {
            Id = Guid.NewGuid(),
            Name = $"User {i}",
            Email = $"user{i}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Student
        }).ToList();
        _context.Users.AddRange(users);

        // 2 Confirmed
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[0].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(9)
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[1].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(10)
        });

        // 3 CheckedIn
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[2].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(8),
            CheckedInAt = baseDate.AddHours(13).AddMinutes(5), // 13:05 -> 13:00 bucket
            CheckedInBy = _organizer.Id
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[3].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(8),
            CheckedInAt = baseDate.AddHours(13).AddMinutes(12), // 13:12 -> 13:00 bucket
            CheckedInBy = _organizer.Id
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[4].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddDays(-1).AddHours(14),
            CheckedInAt = baseDate.AddHours(13).AddMinutes(35), // 13:35 -> 13:30 bucket
            CheckedInBy = _organizer.Id
        });

        // 2 Waitlisted
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[5].Id,
            Status = RegistrationStatus.Waitlisted,
            WaitlistPosition = 1,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(11)
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[6].Id,
            Status = RegistrationStatus.Waitlisted,
            WaitlistPosition = 2,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(11).AddMinutes(30)
        });

        // 1 Cancelled
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = users[7].Id,
            Status = RegistrationStatus.Cancelled,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = baseDate.AddHours(7)
        });

        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetAttendanceStatsAsync(_event.Id, _organizer.Id, isAdmin: false);

        // Assert
        stats.Should().NotBeNull();
        stats.EventId.Should().Be(_event.Id);
        stats.Capacity.Should().Be(50);
        stats.ConfirmedCount.Should().Be(2);
        stats.CheckedInCount.Should().Be(3);
        stats.WaitlistCount.Should().Be(2);
        stats.CancelledCount.Should().Be(1);
        stats.NoShowRate.Should().Be(40.0);

        // Check intervals: 2 in 13:00 bucket, 1 in 13:30 bucket
        stats.CheckInsPer15MinInterval.Should().HaveCount(2);
        stats.CheckInsPer15MinInterval.First(i => i.IntervalStart.Minute == 0).Count.Should().Be(2);
        stats.CheckInsPer15MinInterval.First(i => i.IntervalStart.Minute == 30).Count.Should().Be(1);

        // Check registrations per day: 1 on baseDate - 1, 7 on baseDate
        stats.RegistrationsPerDay.Should().HaveCount(2);
        stats.RegistrationsPerDay.First(d => d.Date == baseDate.AddDays(-1)).Count.Should().Be(1);
        stats.RegistrationsPerDay.First(d => d.Date == baseDate).Count.Should().Be(7);
    }

    [Fact]
    public async Task GetAttendanceStatsAsync_NonOrganizerNonAdmin_ThrowsForbiddenException()
    {
        // Act
        var act = () => _service.GetAttendanceStatsAsync(_event.Id, _student.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ExportAttendeesCsvAsync_GeneratesProperCsvHeaderAndRows()
    {
        // Arrange
        var ticketCode = Guid.NewGuid();
        var reg = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = ticketCode,
            RegisteredAt = new DateTime(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc)
        };
        _context.Registrations.Add(reg);
        await _context.SaveChangesAsync();

        // Act
        var csvBytes = await _service.ExportAttendeesCsvAsync(_event.Id, _organizer.Id, isAdmin: false);
        var csvContent = Encoding.UTF8.GetString(csvBytes);

        // Assert
        csvContent.Should().Contain("TicketCode,Name,Email,Status,WaitlistPosition,RegisteredAtUtc,CheckedInAtUtc,CheckedInBy");
        csvContent.Should().Contain(ticketCode.ToString());
        csvContent.Should().Contain(_student.Name);
        csvContent.Should().Contain(_student.Email);
        csvContent.Should().Contain("Confirmed");
    }

    [Fact]
    public async Task ExportAttendeesCsvAsync_NonOrganizerNonAdmin_ThrowsForbiddenException()
    {
        // Act
        var act = () => _service.ExportAttendeesCsvAsync(_event.Id, _student.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
