using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using Eventloop.Core.Messages;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class RegistrationEmailProcessorTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly RegistrationEmailProcessor _processor;
    private readonly User _student;
    private readonly Event _event;
    private readonly Registration _registration;

    public RegistrationEmailProcessorTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _emailSenderMock = new Mock<IEmailSender>();

        _processor = new RegistrationEmailProcessor(
            _context,
            _emailSenderMock.Object,
            NullLogger<RegistrationEmailProcessor>.Instance);

        var organizer = new User
        {
            Id = Guid.NewGuid(),
            Name = "Organizer",
            Email = "org@college.edu",
            PasswordHash = "h",
            Role = UserRole.Organizer
        };

        _student = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john.doe@college.edu",
            PasswordHash = "h",
            Role = UserRole.Student
        };

        _event = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizer.Id,
            Title = "Spring Fest",
            Description = "Annual college fest",
            Venue = "Main Amphitheater",
            StartTime = DateTime.UtcNow.AddHours(12),
            EndTime = DateTime.UtcNow.AddHours(16),
            Capacity = 100,
            Status = EventStatus.Published
        };

        _registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = _event.Id,
            UserId = _student.Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow
        };

        _context.Users.AddRange(organizer, _student);
        _context.Events.Add(_event);
        _context.Registrations.Add(_registration);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ProcessEventMessageAsync_TicketIssued_SendsConfirmationEmailWithTicketCode()
    {
        // Arrange
        var message = new RegistrationEventMessage(
            RegistrationEventTypes.TicketIssued,
            _registration.Id,
            _student.Id,
            _event.Id);

        // Act
        await _processor.ProcessEventMessageAsync(message);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            _student.Email,
            It.Is<string>(s => s.Contains("Your Ticket")),
            It.Is<string>(b => b.Contains(_registration.TicketCode.ToString()) && b.Contains(_event.Title)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEventMessageAsync_WaitlistPromoted_SendsPromotedEmailWithTicketCode()
    {
        // Arrange
        var message = new RegistrationEventMessage(
            RegistrationEventTypes.WaitlistPromoted,
            _registration.Id,
            _student.Id,
            _event.Id);

        // Act
        await _processor.ProcessEventMessageAsync(message);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            _student.Email,
            It.Is<string>(s => s.Contains("spot opened")),
            It.Is<string>(b => b.Contains("You're off the waitlist!") && b.Contains(_registration.TicketCode.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEventMessageAsync_RegistrationCancelled_SendsCancellationEmail()
    {
        // Arrange
        _registration.Status = RegistrationStatus.Cancelled;
        await _context.SaveChangesAsync();

        var message = new RegistrationEventMessage(
            RegistrationEventTypes.RegistrationCancelled,
            _registration.Id,
            _student.Id,
            _event.Id);

        // Act
        await _processor.ProcessEventMessageAsync(message);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            _student.Email,
            It.Is<string>(s => s.Contains("Registration Cancelled")),
            It.Is<string>(b => b.Contains("cancelled")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEventMessageAsync_WhenTicketIssuedOnCancelledTicket_SkipsSendingDueToIdempotency()
    {
        // Arrange: status changed to Cancelled in DB
        _registration.Status = RegistrationStatus.Cancelled;
        await _context.SaveChangesAsync();

        var message = new RegistrationEventMessage(
            RegistrationEventTypes.TicketIssued,
            _registration.Id,
            _student.Id,
            _event.Id);

        // Act
        await _processor.ProcessEventMessageAsync(message);

        // Assert: No email should be sent
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendDailyRemindersAsync_SendsRemindersOnlyForUpcomingConfirmedEventsAndSetsReminderSentAt()
    {
        // Arrange: _registration is Confirmed, starts in 12 hours (< 24 hours), ReminderSentAt is null

        // Act
        var count = await _processor.SendDailyRemindersAsync();

        // Assert
        count.Should().Be(1);

        _emailSenderMock.Verify(e => e.SendEmailAsync(
            _student.Email,
            It.Is<string>(s => s.Contains("Reminder")),
            It.Is<string>(b => b.Contains(_registration.TicketCode.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);

        var updatedReg = await _context.Registrations.FindAsync(_registration.Id);
        updatedReg!.ReminderSentAt.Should().NotBeNull();
        updatedReg.ReminderSentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SendDailyRemindersAsync_WhenAlreadyReminded_DoesNotSendSecondReminder()
    {
        // Arrange: Mark registration as already reminded
        _registration.ReminderSentAt = DateTime.UtcNow.AddHours(-2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _processor.SendDailyRemindersAsync();

        // Assert
        count.Should().Be(0);

        _emailSenderMock.Verify(e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
