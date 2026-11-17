using Eventloop.Api.Controllers;
using Eventloop.Api.Validators;
using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Events;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CreateEventRequestValidator _createValidator;
    private readonly UpdateEventRequestValidator _updateValidator;
    private readonly EventsController _controller;
    private readonly Guid _organizerId = Guid.NewGuid();

    public EventsControllerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _createValidator = new CreateEventRequestValidator();
        _updateValidator = new UpdateEventRequestValidator();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_organizerId);
        _currentUserServiceMock.Setup(s => s.IsInRole(UserRole.Admin)).Returns(false);

        _controller = new EventsController(
            _eventServiceMock.Object,
            _currentUserServiceMock.Object,
            _createValidator,
            _updateValidator);
    }

    [Fact]
    public async Task CreateEvent_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateEventRequestDto(
            "Game Dev Jam",
            "Annual game jam event",
            "Lab 3",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(3),
            50
        );

        var response = new EventResponseDto(
            Guid.NewGuid(),
            _organizerId,
            "Organizer Name",
            request.Title,
            request.Description,
            request.Venue,
            request.StartTime,
            request.EndTime,
            request.Capacity,
            EventStatus.Draft,
            DateTime.UtcNow,
            0,
            0,
            50,
            false
        );

        _eventServiceMock
            .Setup(s => s.CreateEventAsync(request, _organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateEvent(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task UpdateEvent_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new UpdateEventRequestDto(
            "Game Dev Jam v2",
            "Updated description",
            "Lab 4",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(3),
            60
        );

        var response = new EventResponseDto(
            eventId,
            _organizerId,
            "Organizer Name",
            request.Title,
            request.Description,
            request.Venue,
            request.StartTime,
            request.EndTime,
            request.Capacity,
            EventStatus.Draft,
            DateTime.UtcNow,
            0,
            0,
            60,
            false
        );

        _eventServiceMock
            .Setup(s => s.UpdateEventAsync(eventId, request, _organizerId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateEvent(eventId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task DeleteEvent_WhenValid_ReturnsNoContent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _eventServiceMock
            .Setup(s => s.DeleteEventAsync(eventId, _organizerId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteEvent(eventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PublishEvent_WhenValid_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var response = new EventResponseDto(
            eventId,
            _organizerId,
            "Org",
            "Title",
            "Desc",
            "Venue",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            20,
            EventStatus.Published,
            DateTime.UtcNow,
            0,
            0,
            20,
            false
        );

        _eventServiceMock
            .Setup(s => s.PublishEventAsync(eventId, _organizerId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.PublishEvent(eventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CancelEvent_WhenValid_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var response = new EventResponseDto(
            eventId,
            _organizerId,
            "Org",
            "Title",
            "Desc",
            "Venue",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            20,
            EventStatus.Cancelled,
            DateTime.UtcNow,
            0,
            0,
            20,
            false
        );

        _eventServiceMock
            .Setup(s => s.CancelEventAsync(eventId, _organizerId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CancelEvent(eventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetPublishedEvents_ReturnsOkWithPagedResult()
    {
        // Arrange
        var query = new EventQueryParametersDto();
        var pagedResult = new PagedResultDto<EventResponseDto>([], 0, 1, 10);

        _eventServiceMock
            .Setup(s => s.GetPublishedEventsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetPublishedEvents(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetEventById_ReturnsOkWithDetails()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var response = new EventResponseDto(
            eventId,
            _organizerId,
            "Org",
            "Title",
            "Desc",
            "Venue",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            20,
            EventStatus.Published,
            DateTime.UtcNow,
            5,
            2,
            15,
            false
        );

        _eventServiceMock
            .Setup(s => s.GetEventByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetEventById(eventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }
}
