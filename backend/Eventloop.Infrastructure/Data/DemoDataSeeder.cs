using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventloop.Infrastructure.Data;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public class DemoDataSeeder : IDemoDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if demo seed data is needed...");

        // Check if demo users already exist
        var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@eventloop.edu", cancellationToken);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Demo data already seeded.");
            return;
        }

        _logger.LogInformation("Seeding demo organizers, students, full events with waitlists, and check-in records...");

        // 1. Create Demo Users
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Campus Administrator",
            Email = "admin@eventloop.edu",
            PasswordHash = _passwordHasher.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };

        var organizerRobotics = new User
        {
            Id = Guid.NewGuid(),
            Name = "Robotics & AI Club",
            Email = "robotics@eventloop.edu",
            PasswordHash = _passwordHasher.HashPassword("Organizer@123"),
            Role = UserRole.Organizer,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };

        var organizerAcm = new User
        {
            Id = Guid.NewGuid(),
            Name = "ACM Student Chapter",
            Email = "acm@eventloop.edu",
            PasswordHash = _passwordHasher.HashPassword("Organizer@123"),
            Role = UserRole.Organizer,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };

        var students = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "Alice Johnson", Email = "alice@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new() { Id = Guid.NewGuid(), Name = "Bob Smith", Email = "bob@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-19) },
            new() { Id = Guid.NewGuid(), Name = "Charlie Brown", Email = "charlie@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-18) },
            new() { Id = Guid.NewGuid(), Name = "Diana Prince", Email = "diana@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-17) },
            new() { Id = Guid.NewGuid(), Name = "Evan Wright", Email = "evan@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-16) },
            new() { Id = Guid.NewGuid(), Name = "Fiona Gallagher", Email = "fiona@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new() { Id = Guid.NewGuid(), Name = "George Clark", Email = "george@eventloop.edu", PasswordHash = _passwordHasher.HashPassword("Student@123"), Role = UserRole.Student, CreatedAt = DateTime.UtcNow.AddDays(-14) }
        };

        _context.Users.Add(admin);
        _context.Users.Add(organizerRobotics);
        _context.Users.Add(organizerAcm);
        _context.Users.AddRange(students);

        // 2. Event 1: Full Event with Waitlist
        var fullEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerRobotics.Id,
            Title = "Autonomous Drone Flight Workshop",
            Description = "Hands-on indoor drone piloting, obstacle avoidance with optical sensors, and autonomous waypoint programming. Limited to 3 flight stations.",
            Venue = "Aeronautics Hangar 4B",
            StartTime = DateTime.UtcNow.AddDays(3).Date.AddHours(14),
            EndTime = DateTime.UtcNow.AddDays(3).Date.AddHours(17),
            Capacity = 3,
            Status = EventStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        _context.Events.Add(fullEvent);

        // 3 Confirmed attendees
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = fullEvent.Id,
            UserId = students[0].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-5).AddHours(10)
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = fullEvent.Id,
            UserId = students[1].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-5).AddHours(11)
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = fullEvent.Id,
            UserId = students[2].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-4).AddHours(9)
        });

        // 2 Waitlisted students
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = fullEvent.Id,
            UserId = students[3].Id,
            Status = RegistrationStatus.Waitlisted,
            WaitlistPosition = 1,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-3).AddHours(14)
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = fullEvent.Id,
            UserId = students[4].Id,
            Status = RegistrationStatus.Waitlisted,
            WaitlistPosition = 2,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-2).AddHours(16)
        });

        // 3. Event 2: Event with Live Check-Ins and Attendance Stats
        var hackathonEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerAcm.Id,
            Title = "Annual 24-Hour Campus Hackathon 2026",
            Description = "Join 100+ passionate student hackers to build AI, web, and hardware projects over 24 hours. Free meals, swag, mentors, and prize tracks!",
            Venue = "Grand Student Union Ballroom",
            StartTime = DateTime.UtcNow.Date.AddHours(9),
            EndTime = DateTime.UtcNow.Date.AddHours(21),
            Capacity = 10,
            Status = EventStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _context.Events.Add(hackathonEvent);

        var checkInBase = DateTime.UtcNow.Date.AddHours(9);
        // Checked in attendees with 15-minute interval distribution
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[0].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-8),
            CheckedInAt = checkInBase.AddMinutes(5), // 09:05 -> 09:00 bucket
            CheckedInBy = organizerAcm.Id
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[1].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-7),
            CheckedInAt = checkInBase.AddMinutes(12), // 09:12 -> 09:00 bucket
            CheckedInBy = organizerAcm.Id
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[2].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-6),
            CheckedInAt = checkInBase.AddMinutes(34), // 09:34 -> 09:30 bucket
            CheckedInBy = organizerAcm.Id
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[3].Id,
            Status = RegistrationStatus.CheckedIn,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-5),
            CheckedInAt = checkInBase.AddMinutes(41), // 09:41 -> 09:30 bucket
            CheckedInBy = organizerAcm.Id
        });

        // 2 Confirmed attendees (no-shows or not yet arrived)
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[4].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-4)
        });
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[5].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-3)
        });

        // 1 Cancelled registration
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = hackathonEvent.Id,
            UserId = students[6].Id,
            Status = RegistrationStatus.Cancelled,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-9)
        });

        // 4. Event 3: Upcoming Open Event
        var careerFair = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = admin.Id,
            Title = "Spring Engineering & Tech Career Fair 2026",
            Description = "Connect with over 40 top engineering and technology companies hiring for summer internships and full-time positions. Bring copies of your resume.",
            Venue = "Campus Recreation & Sports Complex",
            StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(10),
            EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(16),
            Capacity = 150,
            Status = EventStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _context.Events.Add(careerFair);

        // Confirmed student for Alice so she has tickets in My Tickets
        _context.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = careerFair.Id,
            UserId = students[0].Id,
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid(),
            RegisteredAt = DateTime.UtcNow.AddDays(-1)
        });

        // 5. Event 4: Draft Event
        var draftEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerRobotics.Id,
            Title = "Intro to ROS2 (Robot Operating System) Seminar",
            Description = "Learn robotics middleware architecture, node communication, topics, and simulation in Gazebo.",
            Venue = "Computer Lab 301",
            StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(15),
            EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(18),
            Capacity = 30,
            Status = EventStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Events.Add(draftEvent);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Demo seed data successfully populated!");
    }
}
