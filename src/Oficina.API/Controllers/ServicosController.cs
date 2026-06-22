using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Servicos;

namespace Oficina.API.Controllers;

/// <summary>CRUD de servicos (catalogo). Requisito: gestao administrativa de servicos.</summary>
[ApiController]
[Authorize]
[Route("api/servicos")]
public class ServicosController : ControllerBase
{
    private readonly ServicoService _service;
    public ServicosController(ServicoService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Criar(CreateServicoRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, UpdateServicoRequest req, CancellationToken ct)
        => Ok(await _service.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _service.RemoverAsync(id, ct);
        return NoContent();
    }
}
