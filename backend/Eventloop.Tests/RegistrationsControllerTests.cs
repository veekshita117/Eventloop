using Eventloop.Api.Controllers;
using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Registrations;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class RegistrationsControllerTests
{
    private readonly Mock<IRegistrationService> _serviceMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly RegistrationsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public RegistrationsControllerTests()
    {
        _serviceMock = new Mock<IRegistrationService>();
        _currentUserMock = new Mock<ICurrentUserService>();

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);

        _controller = new RegistrationsController(_serviceMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Register_ReturnsCreatedResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var response = new RegistrationResponseDto(
            Guid.NewGuid(),
            eventId,
            "Event Title",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            "Venue",
            _userId,
            "Student",
            "student@test.com",
            RegistrationStatus.Confirmed,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow,
            null
        );

        _serviceMock
            .Setup(s => s.RegisterAsync(eventId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Register(eventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objResult = result as ObjectResult;
        objResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        objResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CancelRegistration_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var response = new RegistrationResponseDto(
            Guid.NewGuid(),
            eventId,
            "Event Title",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            "Venue",
            _userId,
            "Student",
            "student@test.com",
            RegistrationStatus.Cancelled,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow,
            null
        );

        _serviceMock
            .Setup(s => s.CancelRegistrationAsync(eventId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CancelRegistration(eventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetMyRegistrations_ReturnsOkWithList()
    {
        // Arrange
        var myRegistrations = new List<MyRegistrationDto>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Hackathon",
                "Hall A",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                RegistrationStatus.Confirmed,
                null,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null
            )
        };

        _serviceMock
            .Setup(s => s.GetUserRegistrationsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(myRegistrations);

        // Act
        var result = await _controller.GetMyRegistrations(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(myRegistrations);
    }

    [Fact]
    public async Task GetEventRegistrations_ReturnsOkWithPagedResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var filters = new RegistrationFilterParametersDto();
        var pagedResult = new PagedResultDto<RegistrationResponseDto>([], 0, 1, 20);

        _serviceMock
            .Setup(s => s.GetEventRegistrationsAsync(eventId, _userId, false, filters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetEventRegistrations(eventId, filters, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }
}
