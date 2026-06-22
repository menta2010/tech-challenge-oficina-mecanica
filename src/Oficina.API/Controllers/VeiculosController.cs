using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Veiculos;

namespace Oficina.API.Controllers;

/// <summary>CRUD de veiculos (placa, marca, modelo, ano). Requisito: cadastro/gestao de veiculos.</summary>
[ApiController]
[Authorize]
[Route("api/veiculos")]
public class VeiculosController : ControllerBase
{
    private readonly VeiculoService _service;
    public VeiculosController(VeiculoService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Criar(CreateVeiculoRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] Guid? clienteId, CancellationToken ct)
        => Ok(await _service.ListarAsync(clienteId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, UpdateVeiculoRequest req, CancellationToken ct)
        => Ok(await _service.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _service.RemoverAsync(id, ct);
        return NoContent();
    }
}
