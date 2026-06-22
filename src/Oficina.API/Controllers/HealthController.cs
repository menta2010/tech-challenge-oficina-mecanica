using Microsoft.AspNetCore.Mvc;

namespace Oficina.API.Controllers;

/// <summary>
/// Endpoint simples de verificacao de disponibilidade da API.
/// Usado pelo docker-compose/healthcheck e como smoke test.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "Oficina.API",
        timestamp = DateTime.UtcNow
    });
}
