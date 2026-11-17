using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Events;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventloop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateEventRequestDto> _createValidator;
    private readonly IValidator<UpdateEventRequestDto> _updateValidator;

    public EventsController(
        IEventService eventService,
        ICurrentUserService currentUserService,
        IValidator<CreateEventRequestDto> createValidator,
        IValidator<UpdateEventRequestDto> updateValidator)
    {
        _eventService = eventService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedEvents([FromQuery] EventQueryParametersDto query, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetPublishedEventsAsync(query, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEvents(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _eventService.GetMyEventsAsync(currentUserId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpPost]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var organizerId = _currentUserService.UserId!.Value;
        var result = await _eventService.CreateEventAsync(request, organizerId, cancellationToken);

        return CreatedAtAction(nameof(GetEventById), new { id = result.Id }, result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(
        [FromRoute] Guid id,
        [FromBody] UpdateEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _eventService.UpdateEventAsync(id, request, currentUserId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        await _eventService.DeleteEventAsync(id, currentUserId, isAdmin, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishEvent([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _eventService.PublishEventAsync(id, currentUserId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEvent([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _eventService.CancelEventAsync(id, currentUserId, isAdmin, cancellationToken);
        return Ok(result);
    }
}
