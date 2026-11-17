using Eventloop.Api.Validators;
using Eventloop.Core.DTOs.Events;
using FluentAssertions;
using Xunit;

namespace Eventloop.Tests;

public class EventValidationTests
{
    private readonly CreateEventRequestValidator _createValidator = new();
    private readonly UpdateEventRequestValidator _updateValidator = new();

    [Fact]
    public async Task CreateEventValidator_WithValidInput_ShouldPassValidation()
    {
        var request = new CreateEventRequestDto(
            "Tech Symposium 2026",
            "Annual engineering and technology conference.",
            "Auditorium 1",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4),
            100
        );

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEventValidator_WithPastStartTime_ShouldFailValidation()
    {
        var request = new CreateEventRequestDto(
            "Past Event",
            "Description",
            "Venue",
            DateTime.UtcNow.AddDays(-1), // in past
            DateTime.UtcNow.AddHours(2),
            50
        );

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequestDto.StartTime));
    }

    [Fact]
    public async Task CreateEventValidator_WithEndTimeBeforeStartTime_ShouldFailValidation()
    {
        var request = new CreateEventRequestDto(
            "Invalid Times Event",
            "Description",
            "Venue",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(1), // before start
            50
        );

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequestDto.EndTime));
    }

    [Fact]
    public async Task CreateEventValidator_WithZeroOrNegativeCapacity_ShouldFailValidation()
    {
        var request = new CreateEventRequestDto(
            "Zero Capacity Event",
            "Description",
            "Venue",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            0 // invalid capacity
        );

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequestDto.Capacity));
    }

    [Fact]
    public async Task UpdateEventValidator_WithEndTimeBeforeStartTime_ShouldFailValidation()
    {
        var request = new UpdateEventRequestDto(
            "Updated Event",
            "Description",
            "Venue",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(1),
            50
        );

        var result = await _updateValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEventRequestDto.EndTime));
    }
}
