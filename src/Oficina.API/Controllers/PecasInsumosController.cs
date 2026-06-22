using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Estoque;

namespace Oficina.API.Controllers;

/// <summary>CRUD de pecas/insumos com controle de estoque. Requisito: gestao de pecas e estoque.</summary>
[ApiController]
[Authorize]
[Route("api/pecas-insumos")]
public class PecasInsumosController : ControllerBase
{
    private readonly PecaInsumoService _service;
    public PecasInsumosController(PecaInsumoService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Criar(CreatePecaInsumoRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, UpdatePecaInsumoRequest req, CancellationToken ct)
        => Ok(await _service.AtualizarAsync(id, req, ct));

    /// <summary>Reabastece o estoque (entrada de pecas/insumos).</summary>
    [HttpPost("{id:guid}/reabastecer")]
    public async Task<IActionResult> Reabastecer(Guid id, ReabastecerEstoqueRequest req, CancellationToken ct)
        => Ok(await _service.ReabastecerAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _service.RemoverAsync(id, ct);
        return NoContent();
    }
}
