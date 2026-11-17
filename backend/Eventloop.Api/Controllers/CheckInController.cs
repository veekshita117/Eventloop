using Eventloop.Core.DTOs.CheckIn;
using Eventloop.Core.DTOs.Stats;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventloop.Api.Controllers;

[ApiController]
public class CheckInController : ControllerBase
{
    private readonly ICheckInService _checkInService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CheckInRequestDto> _checkInValidator;

    public CheckInController(
        ICheckInService checkInService,
        ICurrentUserService currentUserService,
        IValidator<CheckInRequestDto> checkInValidator)
    {
        _checkInService = checkInService;
        _currentUserService = currentUserService;
        _checkInValidator = checkInValidator;
    }

    [Authorize]
    [HttpGet("api/registrations/{id:guid}/ticket")]
    [ProducesResponseType(typeof(TicketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _checkInService.GetTicketDetailsAsync(id, currentUserId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpPost("api/events/{id:guid}/check-in")]
    [ProducesResponseType(typeof(CheckInResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(
        [FromRoute] Guid id,
        [FromBody] CheckInRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _checkInValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var scannerUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _checkInService.CheckInTicketAsync(id, request.TicketCode, scannerUserId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpGet("api/events/{id:guid}/stats")]
    [ProducesResponseType(typeof(EventAttendanceStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttendanceStats([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _checkInService.GetAttendanceStatsAsync(id, currentUserId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpGet("api/events/{id:guid}/attendees.csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportAttendeesCsv([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var bytes = await _checkInService.ExportAttendeesCsvAsync(id, currentUserId, isAdmin, cancellationToken);
        return File(bytes, "text/csv", $"attendees-{id}.csv");
    }
}
