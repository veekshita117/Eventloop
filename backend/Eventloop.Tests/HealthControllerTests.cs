using Eventloop.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Eventloop.Tests;

public class HealthControllerTests
{
    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Eventloop.Api";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public void Get_ShouldReturnHealthyStatus()
    {
        // Arrange
        var testEnv = new TestHostEnvironment();
        var controller = new HealthController(testEnv);

        // Act
        var result = controller.Get() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var value = result.Value;
        value.Should().NotBeNull();
        var statusProp = value!.GetType().GetProperty("status")?.GetValue(value)?.ToString();
        statusProp.Should().Be("Healthy");
    }
}
