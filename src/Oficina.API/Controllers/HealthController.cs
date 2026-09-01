using Microsoft.AspNetCore.Mvc;

namespace Oficina.API.Controllers;

/// <summary>Resposta do health check.</summary>
public sealed record HealthResponse(string Status, string Service, DateTime Timestamp);

/// <summary>
/// Endpoint JSON simples mantido para consulta da disponibilidade da API.
/// As probes e o smoke test utilizam /health/live e /health/ready.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new HealthResponse("ok", "Oficina.API", DateTime.UtcNow));
}
