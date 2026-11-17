using Eventloop.Core.DTOs.User;
using Eventloop.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventloop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [Authorize(Policy = "RequireAdmin")]
    [HttpPost("{id:guid}/promote-organizer")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoteToOrganizer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var updatedUser = await _authService.PromoteToOrganizerAsync(id, cancellationToken);
        return Ok(updatedUser);
    }
}
