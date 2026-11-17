using Microsoft.AspNetCore.Mvc;

namespace Eventloop.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHostEnvironment _env;

    public HealthController(IHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            environment = _env.EnvironmentName,
            timestampUtc = DateTime.UtcNow
        });
    }
}
