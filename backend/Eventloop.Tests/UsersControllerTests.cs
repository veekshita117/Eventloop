using Eventloop.Api.Controllers;
using Eventloop.Core.DTOs.User;
using Eventloop.Core.Enums;
using Eventloop.Core.Exceptions;
using Eventloop.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Eventloop.Tests;

public class UsersControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new UsersController(_authServiceMock.Object);
    }

    [Fact]
    public async Task PromoteToOrganizer_WhenUserExists_ReturnsOkWithUpdatedRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promotedUser = new UserDto(userId, "Jane", "jane@university.edu", UserRole.Organizer, DateTime.UtcNow);

        _authServiceMock
            .Setup(s => s.PromoteToOrganizerAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotedUser);

        // Act
        var result = await _controller.PromoteToOrganizer(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(promotedUser);
    }

    [Fact]
    public async Task PromoteToOrganizer_WhenUserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _authServiceMock
            .Setup(s => s.PromoteToOrganizerAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("User not found."));

        // Act
        var act = async () => await _controller.PromoteToOrganizer(userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
