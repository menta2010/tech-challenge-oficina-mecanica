using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Domain.Identidade;
using Oficina.Application.Clientes;

namespace Oficina.API.Controllers;

/// <summary>CRUD de clientes (identificacao por CPF/CNPJ). Requisito: gestao administrativa de clientes.</summary>
[ApiController]
[Authorize(Roles = Perfis.Funcionarios)]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;
    public ClientesController(ClienteService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Criar(CriarClienteRequest req, CancellationToken ct)
    {
        var r = await _service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = r.Id }, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await _service.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct) => Ok(await _service.ObterAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarClienteRequest req, CancellationToken ct)
        => Ok(await _service.AtualizarAsync(id, req, ct));

    /// <summary>Inativa o cliente: ele deixa de conseguir se autenticar pelo CPF.</summary>
    [HttpPost("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct) => Ok(await _service.InativarAsync(id, ct));

    [HttpPost("{id:guid}/reativar")]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken ct) => Ok(await _service.ReativarAsync(id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _service.RemoverAsync(id, ct);
        return NoContent();
    }
}
