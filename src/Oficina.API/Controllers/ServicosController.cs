using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Domain.Identidade;
using Oficina.Application.Servicos;

namespace Oficina.API.Controllers;

/// <summary>CRUD de servicos (catalogo). Requisito: gestao administrativa de servicos.</summary>
[ApiController]
[Authorize(Roles = Perfis.Funcionarios)]
[Route("api/servicos")]
public class ServicosController : ControllerBase
{
    private readonly ServicoService _service;
    public ServicosController(ServicoService service) => _service = service;

    [Authorize(Roles = Perfis.Admin)]
    [HttpPost]
    public async Task<IActionResult> Criar(CriarServicoRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    [Authorize(Roles = Perfis.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarServicoRequest req, CancellationToken ct)
        => Ok(await _service.AtualizarAsync(id, req, ct));

    [Authorize(Roles = Perfis.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _service.RemoverAsync(id, ct);
        return NoContent();
    }
}
