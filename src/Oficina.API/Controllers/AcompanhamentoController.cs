using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.OrdensServico;

namespace Oficina.API.Controllers;

/// <summary>
/// Consulta publica de andamento da OS pelo cliente (via API), sem alterar estado.
/// Atende ao requisito "permitir consulta por parte do cliente via API". Endpoint anonimo.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/acompanhamento")]
public class AcompanhamentoController : ControllerBase
{
    private readonly OrdemServicoService _service;
    public AcompanhamentoController(OrdemServicoService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
        => Ok(await _service.AcompanharAsync(id, ct));
}
