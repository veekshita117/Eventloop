using Eventloop.Core.DTOs.Auth;
using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using Eventloop.Infrastructure.Data;
using Eventloop.Infrastructure.Security;
using Eventloop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Eventloop.Tests;

public class AuthServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(dbOptions);
        _passwordHasher = new BCryptPasswordHasher();

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "Super-Secret-Key-For-Testing-Only-Must-Be-At-Least-32-Chars!",
            Issuer = "EventloopTestApi",
            Audience = "EventloopTestApp",
            ExpiryInMinutes = 60
        });

        _jwtTokenGenerator = new JwtTokenGenerator(jwtSettings);
        _authService = new AuthService(_context, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task RegisterAsync_WithValidDetails_ShouldCreateStudentByDefaultAndReturnToken()
    {
        // Arrange
        var request = new RegisterRequestDto("John Doe", "john@university.edu", "Password123!");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.User.Name.Should().Be("John Doe");
        result.User.Email.Should().Be("john@university.edu");
        result.User.Role.Should().Be(UserRole.Student);

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@university.edu");
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Student);
        _passwordHasher.VerifyPassword("Password123!", savedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowConflictException()
    {
        // Arrange
        var request1 = new RegisterRequestDto("John Doe", "duplicate@university.edu", "Password123!");
        await _authService.RegisterAsync(request1);

        var request2 = new RegisterRequestDto("Jane Doe", "DUPLICATE@university.edu", "AnotherPassword!");

        // Act
        var act = async () => await _authService.RegisterAsync(request2);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokenAndUser()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto("Alice Smith", "alice@university.edu", "AliceSecurePass!");
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequestDto("alice@university.edu", "AliceSecurePass!");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("alice@university.edu");
        result.User.Role.Should().Be(UserRole.Student);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto("Bob", "bob@university.edu", "CorrectPass123!");
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequestDto("bob@university.edu", "WrongPassword!");

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto("unknown@university.edu", "Password123!");

        // Act
        var act = async () => await _authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var reg = await _authService.RegisterAsync(new RegisterRequestDto("Charlie", "charlie@university.edu", "Pass123!"));

        // Act
        var user = await _authService.GetCurrentUserAsync(reg.User.Id);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().Be(reg.User.Id);
        user.Email.Should().Be("charlie@university.edu");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Act
        var act = async () => await _authService.GetCurrentUserAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task PromoteToOrganizerAsync_WithExistingStudent_ShouldChangeRoleToOrganizer()
    {
        // Arrange
        var reg = await _authService.RegisterAsync(new RegisterRequestDto("David", "david@university.edu", "Pass123!"));

        // Act
        var updated = await _authService.PromoteToOrganizerAsync(reg.User.Id);

        // Assert
        updated.Should().NotBeNull();
        updated.Role.Should().Be(UserRole.Organizer);

        var dbUser = await _context.Users.FindAsync(reg.User.Id);
        dbUser!.Role.Should().Be(UserRole.Organizer);
    }

    [Fact]
    public async Task PromoteToOrganizerAsync_WithNonExistentUser_ShouldThrowNotFoundException()
    {
        // Act
        var act = async () => await _authService.PromoteToOrganizerAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
