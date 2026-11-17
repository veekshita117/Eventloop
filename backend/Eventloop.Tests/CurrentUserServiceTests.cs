using System.Security.Claims;
using Eventloop.Api.Services;
using Eventloop.Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Eventloop.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void Properties_WhenUserIsAuthenticated_ShouldReturnCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "student@college.edu"),
            new(ClaimTypes.Role, UserRole.Organizer.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = user };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var service = new CurrentUserService(httpContextAccessor);

        // Assert
        service.IsAuthenticated.Should().BeTrue();
        service.UserId.Should().Be(userId);
        service.Email.Should().Be("student@college.edu");
        service.Role.Should().Be(UserRole.Organizer);
        service.IsInRole(UserRole.Organizer).Should().BeTrue();
        service.IsInRole(UserRole.Student).Should().BeFalse();
    }

    [Fact]
    public void Properties_WhenNotAuthenticated_ShouldReturnNullsAndFalse()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var service = new CurrentUserService(httpContextAccessor);

        // Assert
        service.IsAuthenticated.Should().BeFalse();
        service.UserId.Should().BeNull();
        service.Email.Should().BeNull();
        service.Role.Should().BeNull();
        service.IsInRole(UserRole.Student).Should().BeFalse();
    }
}
