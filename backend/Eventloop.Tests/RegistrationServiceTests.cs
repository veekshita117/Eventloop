using Eventloop.Core.DTOs.Events;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class RegistrationServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IRegistrationEventPublisher> _publisherMock;
    private readonly RegistrationService _registrationService;
    private readonly EventService _eventService;
    private readonly User _organizer;
    private readonly Event _testEvent;

    public RegistrationServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _publisherMock = new Mock<IRegistrationEventPublisher>();

        _registrationService = new RegistrationService(_context, _publisherMock.Object);
        _eventService = new EventService(_context, _registrationService);

        _organizer = new User
        {
            Id = Guid.NewGuid(),
            Name = "Club President",
            Email = "president@college.edu",
            PasswordHash = "hash",
            Role = UserRole.Organizer
        };

        _testEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = _organizer.Id,
            Title = "Tech Talk",
            Description = "Tech talk description",
            Venue = "Room 101",
            StartTime = DateTime.UtcNow.AddDays(3),
            EndTime = DateTime.UtcNow.AddDays(3).AddHours(2),
            Capacity = 3,
            Status = EventStatus.Published
        };

        _context.Users.Add(_organizer);
        _context.Events.Add(_testEvent);
        _context.SaveChanges();
    }

    private User CreateStudent(string name, string email)
    {
        var student = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = "hash",
            Role = UserRole.Student
        };
        _context.Users.Add(student);
        _context.SaveChanges();
        return student;
    }

    [Fact]
    public async Task RegisterAsync_WhenSpotsAvailable_CreatesConfirmedRegistrationWithTicketCode()
    {
        // Arrange
        var student = CreateStudent("Alice", "alice@college.edu");

        // Act
        var result = await _registrationService.RegisterAsync(_testEvent.Id, student.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(RegistrationStatus.Confirmed);
        result.WaitlistPosition.Should().BeNull();
        result.TicketCode.Should().NotBe(Guid.Empty);

        _publisherMock.Verify(p => p.PublishTicketIssuedAsync(
            It.Is<Registration>(r => r.UserId == student.Id && r.Status == RegistrationStatus.Confirmed),
            It.IsAny<Event>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEventIsFull_PlacesUserOnWaitlistWithSequentialPosition()
    {
        // Arrange: Fill all 3 spots
        var s1 = CreateStudent("S1", "s1@college.edu");
        var s2 = CreateStudent("S2", "s2@college.edu");
        var s3 = CreateStudent("S3", "s3@college.edu");
        await _registrationService.RegisterAsync(_testEvent.Id, s1.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s2.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s3.Id);

        var s4 = CreateStudent("S4", "s4@college.edu");
        var s5 = CreateStudent("S5", "s5@college.edu");

        // Act
        var reg4 = await _registrationService.RegisterAsync(_testEvent.Id, s4.Id);
        var reg5 = await _registrationService.RegisterAsync(_testEvent.Id, s5.Id);

        // Assert
        reg4.Status.Should().Be(RegistrationStatus.Waitlisted);
        reg4.WaitlistPosition.Should().Be(1);

        reg5.Status.Should().Be(RegistrationStatus.Waitlisted);
        reg5.WaitlistPosition.Should().Be(2);
    }

    [Fact]
    public async Task RegisterAsync_WhenUserRegistersTwice_ThrowsConflictException()
    {
        // Arrange
        var student = CreateStudent("Bob", "bob@college.edu");
        await _registrationService.RegisterAsync(_testEvent.Id, student.Id);

        // Act: Register again
        var act = async () => await _registrationService.RegisterAsync(_testEvent.Id, student.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task CancelRegistrationAsync_WhenConfirmedRegistrationCancelled_PromotesFirstWaitlistedUser()
    {
        // Arrange: Fill 3 spots and add 2 on waitlist
        var s1 = CreateStudent("S1", "s1@college.edu");
        var s2 = CreateStudent("S2", "s2@college.edu");
        var s3 = CreateStudent("S3", "s3@college.edu");
        var s4 = CreateStudent("S4 (Wait 1)", "s4@college.edu");
        var s5 = CreateStudent("S5 (Wait 2)", "s5@college.edu");

        await _registrationService.RegisterAsync(_testEvent.Id, s1.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s2.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s3.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s4.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s5.Id);

        // Act: Cancel confirmed registration S1
        var cancelResult = await _registrationService.CancelRegistrationAsync(_testEvent.Id, s1.Id);

        // Assert S1 is cancelled
        cancelResult.Status.Should().Be(RegistrationStatus.Cancelled);

        // Assert S4 (Waitlist 1) is now Confirmed
        var reg4 = await _context.Registrations.FirstAsync(r => r.UserId == s4.Id);
        reg4.Status.Should().Be(RegistrationStatus.Confirmed);
        reg4.WaitlistPosition.Should().BeNull();

        // Assert S5 (Waitlist 2) renumbered to position 1
        var reg5 = await _context.Registrations.FirstAsync(r => r.UserId == s5.Id);
        reg5.Status.Should().Be(RegistrationStatus.Waitlisted);
        reg5.WaitlistPosition.Should().Be(1);

        _publisherMock.Verify(p => p.PublishWaitlistPromotedAsync(
            It.Is<Registration>(r => r.UserId == s4.Id),
            It.IsAny<Event>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelRegistrationAsync_WhenWaitlistedUserCancels_RenumbersRemainingPositions()
    {
        // Arrange: Fill 3 spots and add 3 on waitlist
        var s1 = CreateStudent("S1", "s1@college.edu");
        var s2 = CreateStudent("S2", "s2@college.edu");
        var s3 = CreateStudent("S3", "s3@college.edu");
        var w1 = CreateStudent("W1", "w1@college.edu");
        var w2 = CreateStudent("W2", "w2@college.edu");
        var w3 = CreateStudent("W3", "w3@college.edu");

        await _registrationService.RegisterAsync(_testEvent.Id, s1.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s2.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, s3.Id);
        await _registrationService.RegisterAsync(_testEvent.Id, w1.Id); // pos 1
        await _registrationService.RegisterAsync(_testEvent.Id, w2.Id); // pos 2
        await _registrationService.RegisterAsync(_testEvent.Id, w3.Id); // pos 3

        // Act: Cancel waitlisted user W2 (pos 2)
        await _registrationService.CancelRegistrationAsync(_testEvent.Id, w2.Id);

        // Assert
        var regW1 = await _context.Registrations.FirstAsync(r => r.UserId == w1.Id);
        regW1.WaitlistPosition.Should().Be(1);

        var regW2 = await _context.Registrations.FirstAsync(r => r.UserId == w2.Id);
        regW2.Status.Should().Be(RegistrationStatus.Cancelled);
        regW2.WaitlistPosition.Should().BeNull();

        var regW3 = await _context.Registrations.FirstAsync(r => r.UserId == w3.Id);
        regW3.WaitlistPosition.Should().Be(2); // renumbered from 3 to 2
    }

    [Fact]
    public async Task UpdateEvent_WhenCapacityRaised_AutomaticallyPromotesWaitlistedUsers()
    {
        // Arrange: Event with capacity 2, 2 confirmed and 2 waitlisted
        var smallEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = _organizer.Id,
            Title = "Small Event",
            Description = "Desc",
            Venue = "Hall",
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            Capacity = 2,
            Status = EventStatus.Published
        };
        _context.Events.Add(smallEvent);
        await _context.SaveChangesAsync();

        var s1 = CreateStudent("C1", "c1@test.com");
        var s2 = CreateStudent("C2", "c2@test.com");
        var w1 = CreateStudent("W1", "w1@test.com");
        var w2 = CreateStudent("W2", "w2@test.com");

        await _registrationService.RegisterAsync(smallEvent.Id, s1.Id);
        await _registrationService.RegisterAsync(smallEvent.Id, s2.Id);
        await _registrationService.RegisterAsync(smallEvent.Id, w1.Id);
        await _registrationService.RegisterAsync(smallEvent.Id, w2.Id);

        // Act: Increase capacity from 2 to 3 (+1 spot)
        var updateRequest = new UpdateEventRequestDto(
            smallEvent.Title,
            smallEvent.Description,
            smallEvent.Venue,
            smallEvent.StartTime,
            smallEvent.EndTime,
            Capacity: 3
        );

        await _eventService.UpdateEventAsync(smallEvent.Id, updateRequest, _organizer.Id, isAdmin: false);

        // Assert: W1 should be promoted to Confirmed, W2 should be waitlisted position 1
        var regW1 = await _context.Registrations.FirstAsync(r => r.UserId == w1.Id);
        regW1.Status.Should().Be(RegistrationStatus.Confirmed);
        regW1.WaitlistPosition.Should().BeNull();

        var regW2 = await _context.Registrations.FirstAsync(r => r.UserId == w2.Id);
        regW2.Status.Should().Be(RegistrationStatus.Waitlisted);
        regW2.WaitlistPosition.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrencyTest_MultipleParallelRegistrations_ExactlyCapacitySpotsConfirmed()
    {
        // Arrange: Event with capacity = 5 spots
        var capacityEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = _organizer.Id,
            Title = "High Demand Workshop",
            Description = "Limited seating",
            Venue = "Lab 1",
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(3),
            Capacity = 5,
            Status = EventStatus.Published
        };
        _context.Events.Add(capacityEvent);
        await _context.SaveChangesAsync();

        // 20 students attempting to register at the same time
        const int totalCandidates = 20;
        var candidates = new List<User>();
        for (int i = 0; i < totalCandidates; i++)
        {
            var student = new User
            {
                Id = Guid.NewGuid(),
                Name = $"Candidate {i}",
                Email = $"candidate{i}@concurrent.edu",
                PasswordHash = "hash",
                Role = UserRole.Student
            };
            candidates.Add(student);
            _context.Users.Add(student);
        }
        await _context.SaveChangesAsync();

        // Act: Fire 20 registration requests in parallel
        var tasks = candidates.Select(c => _registrationService.RegisterAsync(capacityEvent.Id, c.Id));
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(20);

        var confirmedRegistrations = await _context.Registrations
            .Where(r => r.EventId == capacityEvent.Id && r.Status == RegistrationStatus.Confirmed)
            .ToListAsync();

        var waitlistedRegistrations = await _context.Registrations
            .Where(r => r.EventId == capacityEvent.Id && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.WaitlistPosition)
            .ToListAsync();

        // Exactly 5 spots confirmed (no overbooking under race conditions)
        confirmedRegistrations.Should().HaveCount(5);
        confirmedRegistrations.All(r => r.TicketCode != Guid.Empty).Should().BeTrue();
        confirmedRegistrations.All(r => r.WaitlistPosition == null).Should().BeTrue();

        // Exactly 15 on the waitlist
        waitlistedRegistrations.Should().HaveCount(15);

        // Waitlist positions must be continuous 1 through 15 without gaps or duplicates
        var expectedPositions = Enumerable.Range(1, 15).ToList();
        waitlistedRegistrations.Select(r => r.WaitlistPosition!.Value).Should().Equal(expectedPositions);
    }
}
