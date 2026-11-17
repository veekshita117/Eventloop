using Eventloop.Api.Controllers;
using Eventloop.Api.Validators;
using Eventloop.Core.DTOs.CheckIn;
using Eventloop.Core.DTOs.Stats;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class CheckInControllerTests
{
    private readonly Mock<ICheckInService> _serviceMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly CheckInRequestValidator _validator;
    private readonly CheckInController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public CheckInControllerTests()
    {
        _serviceMock = new Mock<ICheckInService>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _validator = new CheckInRequestValidator();

        _currentUserMock.Setup(u => u.UserId).Returns(_userId);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);

        _controller = new CheckInController(_serviceMock.Object, _currentUserMock.Object, _validator);
    }

    [Fact]
    public async Task GetTicket_ReturnsOkWithTicketDetails()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var ticketDto = new TicketDetailsDto(
            registrationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Hackathon",
            "Main Hall",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            "Alice",
            "alice@test.com",
            RegistrationStatus.Confirmed,
            DateTime.UtcNow,
            null
        );

        _serviceMock
            .Setup(s => s.GetTicketDetailsAsync(registrationId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketDto);

        // Act
        var result = await _controller.GetTicket(registrationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(ticketDto);
    }

    [Fact]
    public async Task CheckIn_ValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var ticketCode = Guid.NewGuid();
        var request = new CheckInRequestDto(ticketCode);

        var checkInResponse = new CheckInResponseDto(
            true,
            "Check-in successful.",
            Guid.NewGuid(),
            ticketCode,
            "Alice",
            "alice@test.com",
            "Hackathon",
            DateTime.UtcNow,
            _userId
        );

        _serviceMock
            .Setup(s => s.CheckInTicketAsync(eventId, ticketCode, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkInResponse);

        // Act
        var result = await _controller.CheckIn(eventId, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(checkInResponse);
    }

    [Fact]
    public async Task CheckIn_EmptyTicketCode_ThrowsValidationException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new CheckInRequestDto(Guid.Empty);

        // Act
        var act = () => _controller.CheckIn(eventId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetAttendanceStats_ReturnsOkWithStats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var statsDto = new EventAttendanceStatsDto(
            eventId,
            "Hackathon",
            100,
            10,
            40,
            5,
            2,
            20.0,
            new List<CheckInIntervalStatDto>(),
            new List<DailyRegistrationStatDto>()
        );

        _serviceMock
            .Setup(s => s.GetAttendanceStatsAsync(eventId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsDto);

        // Act
        var result = await _controller.GetAttendanceStats(eventId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(statsDto);
    }

    [Fact]
    public async Task ExportAttendeesCsv_ReturnsFileContentResultWithCsv()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var csvBytes = "TicketCode,Name\r\n123,Alice"u8.ToArray();

        _serviceMock
            .Setup(s => s.ExportAttendeesCsvAsync(eventId, _userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(csvBytes);

        // Act
        var result = await _controller.ExportAttendeesCsv(eventId, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be($"attendees-{eventId}.csv");
        fileResult.FileContents.Should().BeEquivalentTo(csvBytes);
    }
}
