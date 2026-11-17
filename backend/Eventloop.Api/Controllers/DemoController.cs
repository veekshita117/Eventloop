using Eventloop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventloop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly IDemoDataSeeder _seeder;

    public DemoController(IDemoDataSeeder seeder)
    {
        _seeder = seeder;
    }

    [AllowAnonymous]
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedDemoData(CancellationToken cancellationToken)
    {
        await _seeder.SeedAsync(cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Demo data successfully seeded!",
            credentials = new[]
            {
                new { role = "Admin", email = "admin@eventloop.edu", password = "Admin@123" },
                new { role = "Organizer (Robotics)", email = "robotics@eventloop.edu", password = "Organizer@123" },
                new { role = "Organizer (ACM)", email = "acm@eventloop.edu", password = "Organizer@123" },
                new { role = "Student (Alice)", email = "alice@eventloop.edu", password = "Student@123" },
                new { role = "Student (Bob)", email = "bob@eventloop.edu", password = "Student@123" },
                new { role = "Student (Diana - Waitlisted)", email = "diana@eventloop.edu", password = "Student@123" },
            }
        });
    }
}
