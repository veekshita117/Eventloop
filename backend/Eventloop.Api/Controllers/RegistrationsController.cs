using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Registrations;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventloop.Api.Controllers;

[ApiController]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ICurrentUserService _currentUserService;

    public RegistrationsController(
        IRegistrationService registrationService,
        ICurrentUserService currentUserService)
    {
        _registrationService = registrationService;
        _currentUserService = currentUserService;
    }

    [Authorize]
    [HttpPost("api/events/{id:guid}/register")]
    [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var result = await _registrationService.RegisterAsync(id, userId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [Authorize]
    [HttpDelete("api/events/{id:guid}/register")]
    [ProducesResponseType(typeof(RegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRegistration([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var result = await _registrationService.CancelRegistrationAsync(id, userId, cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("api/me/registrations")]
    [ProducesResponseType(typeof(IReadOnlyList<MyRegistrationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRegistrations(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var result = await _registrationService.GetUserRegistrationsAsync(userId, cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = "RequireOrganizer")]
    [HttpGet("api/events/{id:guid}/registrations")]
    [ProducesResponseType(typeof(PagedResultDto<RegistrationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventRegistrations(
        [FromRoute] Guid id,
        [FromQuery] RegistrationFilterParametersDto filters,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId!.Value;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        var result = await _registrationService.GetEventRegistrationsAsync(id, currentUserId, isAdmin, filters, cancellationToken);
        return Ok(result);
    }
}
