using System.Security.Claims;
using Eventloop.Api.Controllers;
using Eventloop.Api.Validators;
using Eventloop.Core.DTOs.Auth;
using Eventloop.Core.DTOs.User;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly RegisterRequestValidator _registerValidator;
    private readonly LoginRequestValidator _loginValidator;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _registerValidator = new RegisterRequestValidator();
        _loginValidator = new LoginRequestValidator();

        _controller = new AuthController(
            _authServiceMock.Object,
            _currentUserServiceMock.Object,
            _registerValidator,
            _loginValidator);
    }

    [Fact]
    public async Task Register_WithValidInput_Returns201Created()
    {
        // Arrange
        var request = new RegisterRequestDto("John Doe", "john@university.edu", "Password123!");
        var expectedResponse = new AuthResponseDto(
            "jwt-token",
            new UserDto(Guid.NewGuid(), "John Doe", "john@university.edu", UserRole.Student, DateTime.UtcNow));

        _authServiceMock
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objResult = result as ObjectResult;
        objResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        objResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequestDto("John Doe", "duplicate@university.edu", "Password123!");

        _authServiceMock
            .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("A user with this email already exists."));

        // Act
        var act = async () => await _controller.Register(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OkWithToken()
    {
        // Arrange
        var request = new LoginRequestDto("john@university.edu", "Password123!");
        var expectedResponse = new AuthResponseDto(
            "jwt-token",
            new UserDto(Guid.NewGuid(), "John Doe", "john@university.edu", UserRole.Student, DateTime.UtcNow));

        _authServiceMock
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequestDto("john@university.edu", "WrongPassword!");

        _authServiceMock
            .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Invalid email or password."));

        // Act
        var act = async () => await _controller.Login(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Me_WhenAuthenticated_Returns200OkWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new UserDto(userId, "John Doe", "john@university.edu", UserRole.Student, DateTime.UtcNow);

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _authServiceMock
            .Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.Me(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUser);
    }

    [Fact]
    public async Task Me_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        // Act
        var result = await _controller.Me(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }
}
