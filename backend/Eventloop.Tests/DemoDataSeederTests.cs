using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class DemoDataSeederTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly DemoDataSeeder _seeder;

    public DemoDataSeederTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _hasherMock = new Mock<IPasswordHasher>();
        _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed_pwd");

        _seeder = new DemoDataSeeder(_context, _hasherMock.Object, NullLogger<DemoDataSeeder>.Instance);
    }

    [Fact]
    public async Task SeedAsync_PopulatesSampleUsersEventsAndRegistrations()
    {
        // Act
        await _seeder.SeedAsync();

        // Assert
        var users = await _context.Users.ToListAsync();
        users.Should().Contain(u => u.Email == "admin@eventloop.edu" && u.Role == UserRole.Admin);
        users.Should().Contain(u => u.Email == "robotics@eventloop.edu" && u.Role == UserRole.Organizer);
        users.Should().Contain(u => u.Email == "alice@eventloop.edu" && u.Role == UserRole.Student);

        var events = await _context.Events.ToListAsync();
        events.Should().Contain(e => e.Title.Contains("Autonomous Drone") && e.Capacity == 3);
        events.Should().Contain(e => e.Title.Contains("Hackathon 2026"));
        events.Should().Contain(e => e.Title.Contains("Career Fair 2026"));
        events.Should().Contain(e => e.Status == EventStatus.Draft);

        var fullEvent = await _context.Events.FirstAsync(e => e.Capacity == 3);
        var registrations = await _context.Registrations.Where(r => r.EventId == fullEvent.Id).ToListAsync();
        registrations.Count(r => r.Status == RegistrationStatus.Confirmed).Should().Be(3);
        registrations.Count(r => r.Status == RegistrationStatus.Waitlisted).Should().Be(2);

        var hackathon = await _context.Events.FirstAsync(e => e.Title.Contains("Hackathon 2026"));
        var hackathonRegs = await _context.Registrations.Where(r => r.EventId == hackathon.Id).ToListAsync();
        hackathonRegs.Count(r => r.Status == RegistrationStatus.CheckedIn).Should().Be(4);
        hackathonRegs.Count(r => r.Status == RegistrationStatus.Confirmed).Should().Be(2);
        hackathonRegs.Count(r => r.Status == RegistrationStatus.Cancelled).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_DoesNotDuplicateOnSecondRun()
    {
        // Act
        await _seeder.SeedAsync();
        var initialUserCount = await _context.Users.CountAsync();
        var initialEventCount = await _context.Events.CountAsync();

        await _seeder.SeedAsync();
        var secondUserCount = await _context.Users.CountAsync();
        var secondEventCount = await _context.Events.CountAsync();

        // Assert
        secondUserCount.Should().Be(initialUserCount);
        secondEventCount.Should().Be(initialEventCount);
    }
}
