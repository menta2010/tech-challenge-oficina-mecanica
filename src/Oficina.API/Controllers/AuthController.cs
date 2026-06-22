using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Identidade;

namespace Oficina.API.Controllers;

/// <summary>Autenticacao. Emite o JWT usado pelas APIs administrativas. Endpoint publico.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _service;
    public AuthController(AuthService service) => _service = service;

    /// <summary>Login do usuario administrativo. Retorna o token JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
        => Ok(await _service.LoginAsync(req, ct));
}
