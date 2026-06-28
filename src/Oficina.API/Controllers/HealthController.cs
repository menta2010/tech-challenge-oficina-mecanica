using Microsoft.AspNetCore.Mvc;

namespace Oficina.API.Controllers;

/// <summary>Resposta do health check.</summary>
public sealed record HealthResponse(string Status, string Service, DateTime Timestamp);

/// <summary>
/// Endpoint simples de verificacao de disponibilidade da API.
/// Usado pelo docker-compose/healthcheck e como smoke test.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new HealthResponse("ok", "Oficina.API", DateTime.UtcNow));
}
