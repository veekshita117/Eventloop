using Eventloop.Core.DTOs.Events;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Eventloop.Tests;

public class EventServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly EventService _service;
    private readonly User _organizer1;
    private readonly User _organizer2;
    private readonly User _admin;

    public EventServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _service = new EventService(_context);

        _organizer1 = new User
        {
            Id = Guid.NewGuid(),
            Name = "Organizer One",
            Email = "org1@college.edu",
            PasswordHash = "hash",
            Role = UserRole.Organizer
        };

        _organizer2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "Organizer Two",
            Email = "org2@college.edu",
            PasswordHash = "hash",
            Role = UserRole.Organizer
        };

        _admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin User",
            Email = "admin@college.edu",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };

        _context.Users.AddRange(_organizer1, _organizer2, _admin);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateEventAsync_ShouldStartAsDraft()
    {
        // Arrange
        var request = new CreateEventRequestDto(
            "Hackathon 2026",
            "Annual 24-hour coding marathon",
            "Tech Hall A",
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(6),
            100
        );

        // Act
        var result = await _service.CreateEventAsync(request, _organizer1.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(EventStatus.Draft);
        result.OrganizerId.Should().Be(_organizer1.Id);
        result.OrganizerName.Should().Be(_organizer1.Name);
        result.Capacity.Should().Be(100);
        result.SpotsLeft.Should().Be(100);
        result.ConfirmedCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateEventAsync_WhenUserIsOwner_ShouldUpdateSuccessfully()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Orig Title", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 50),
            _organizer1.Id);

        var updateRequest = new UpdateEventRequestDto("New Title", "New Desc", "Hall 2", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 75);

        // Act
        var updated = await _service.UpdateEventAsync(ev.Id, updateRequest, _organizer1.Id, isAdmin: false);

        // Assert
        updated.Title.Should().Be("New Title");
        updated.Capacity.Should().Be(75);
    }

    [Fact]
    public async Task UpdateEventAsync_WhenUserIsNotOwnerAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Org1 Event", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 50),
            _organizer1.Id);

        var updateRequest = new UpdateEventRequestDto("Hacked Title", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 50);

        // Act
        var act = async () => await _service.UpdateEventAsync(ev.Id, updateRequest, _organizer2.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task UpdateEventAsync_WhenUserIsAdmin_ShouldUpdateSuccessfullyEvenIfNotOwner()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Org1 Event", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 50),
            _organizer1.Id);

        var updateRequest = new UpdateEventRequestDto("Admin Edited Title", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 50);

        // Act
        var updated = await _service.UpdateEventAsync(ev.Id, updateRequest, _admin.Id, isAdmin: true);

        // Assert
        updated.Title.Should().Be("Admin Edited Title");
    }

    [Fact]
    public async Task UpdateEventAsync_WhenLoweringCapacityBelowConfirmedCount_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Popular Event", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 10),
            _organizer1.Id);

        // Add 5 confirmed registrations
        for (int i = 0; i < 5; i++)
        {
            var student = new User { Id = Guid.NewGuid(), Name = $"Student {i}", Email = $"student{i}@test.com", PasswordHash = "hash" };
            _context.Users.Add(student);
            _context.Registrations.Add(new Registration
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                UserId = student.Id,
                Status = RegistrationStatus.Confirmed,
                TicketCode = Guid.NewGuid()
            });
        }
        await _context.SaveChangesAsync();

        // Attempt to lower capacity to 4 (below 5 confirmed)
        var updateRequest = new UpdateEventRequestDto("Popular Event", "Desc", "Hall 1", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 4);

        // Act
        var act = async () => await _service.UpdateEventAsync(ev.Id, updateRequest, _organizer1.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Capacity cannot be reduced below the number of confirmed registrations (5)*");
    }

    [Fact]
    public async Task DeleteEventAsync_WhenDraft_ShouldDeleteSuccessfully()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Draft Event", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 20),
            _organizer1.Id);

        // Act
        await _service.DeleteEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Assert
        var exists = await _context.Events.AnyAsync(e => e.Id == ev.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEventAsync_WhenPublished_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Published Event", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 20),
            _organizer1.Id);

        await _service.PublishEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Act
        var act = async () => await _service.DeleteEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only draft events can be deleted*");
    }

    [Fact]
    public async Task DeleteEventAsync_WhenNotOwnerAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Draft Event", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 20),
            _organizer1.Id);

        // Act
        var act = async () => await _service.DeleteEventAsync(ev.Id, _organizer2.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task PublishEventAsync_WhenDraft_ShouldPublishSuccessfully()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Event", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 30),
            _organizer1.Id);

        // Act
        var published = await _service.PublishEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Assert
        published.Status.Should().Be(EventStatus.Published);
    }

    [Fact]
    public async Task PublishEventAsync_WhenAlreadyPublished_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Event", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 30),
            _organizer1.Id);
        await _service.PublishEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Act
        var act = async () => await _service.PublishEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only draft events can be published*");
    }

    [Fact]
    public async Task CancelEventAsync_WhenPublished_ShouldCancelSuccessfully()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Event", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 30),
            _organizer1.Id);
        await _service.PublishEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Act
        var cancelled = await _service.CancelEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Assert
        cancelled.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public async Task GetPublishedEventsAsync_ShouldOnlyReturnPublishedEventsWithFilters()
    {
        // Arrange: Create 1 draft and 2 published events
        var draft = await _service.CreateEventAsync(
            new CreateEventRequestDto("Draft Robotics Expo", "Desc", "Hall", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 50),
            _organizer1.Id);

        var pub1 = await _service.CreateEventAsync(
            new CreateEventRequestDto("Robotics Workshop", "Desc", "Hall", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 30),
            _organizer1.Id);
        await _service.PublishEventAsync(pub1.Id, _organizer1.Id, isAdmin: false);

        var pub2 = await _service.CreateEventAsync(
            new CreateEventRequestDto("AI Guest Lecture", "Desc", "Hall", DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(11), 40),
            _organizer1.Id);
        await _service.PublishEventAsync(pub2.Id, _organizer1.Id, isAdmin: false);

        // Act 1: Get all published
        var resultAll = await _service.GetPublishedEventsAsync(new EventQueryParametersDto());
        resultAll.Items.Should().HaveCount(2);
        resultAll.Items.Select(e => e.Id).Should().NotContain(draft.Id);

        // Act 2: Search by title "Robotics"
        var searchResult = await _service.GetPublishedEventsAsync(new EventQueryParametersDto { Search = "Robotics" });
        searchResult.Items.Should().HaveCount(1);
        searchResult.Items.First().Title.Should().Be("Robotics Workshop");
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldCalculateSpotsLeftAndCountsCorrectly()
    {
        // Arrange
        var ev = await _service.CreateEventAsync(
            new CreateEventRequestDto("Music Fest", "Desc", "Open Ground", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 10),
            _organizer1.Id);
        await _service.PublishEventAsync(ev.Id, _organizer1.Id, isAdmin: false);

        // Add 7 confirmed and 3 waitlisted
        for (int i = 0; i < 7; i++)
        {
            var user = new User { Id = Guid.NewGuid(), Name = $"User {i}", Email = $"u{i}@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            _context.Registrations.Add(new Registration
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                UserId = user.Id,
                Status = RegistrationStatus.Confirmed,
                TicketCode = Guid.NewGuid()
            });
        }

        for (int i = 0; i < 3; i++)
        {
            var user = new User { Id = Guid.NewGuid(), Name = $"Wait {i}", Email = $"w{i}@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            _context.Registrations.Add(new Registration
            {
                Id = Guid.NewGuid(),
                EventId = ev.Id,
                UserId = user.Id,
                Status = RegistrationStatus.Waitlisted,
                WaitlistPosition = i + 1,
                TicketCode = Guid.NewGuid()
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var details = await _service.GetEventByIdAsync(ev.Id);

        // Assert
        details.ConfirmedCount.Should().Be(7);
        details.WaitlistCount.Should().Be(3);
        details.SpotsLeft.Should().Be(3); // 10 capacity - 7 confirmed
        details.IsSoldOut.Should().BeFalse();
    }
}
